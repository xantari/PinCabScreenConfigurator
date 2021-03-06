﻿using PinCab.Utils.Extensions;
using PinCab.Utils.Models;
using PinCab.Utils.Models.PinballX;
using PinCab.Utils.Utils.PinballX;
using PinCab.Utils.ViewModels;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PinCab.Utils.Utils
{
    public class FrontEndManager
    {
        private ProgramSettings _settings = new ProgramSettings();
        private readonly ProgramSettingsManager _settingManager = new ProgramSettingsManager();
        private readonly PinballXManager _pinballXManager = null;
        private readonly List<PinballXSystem> _pinballXSystems = null;
        private readonly string ToolName = "Front End Manager";

        public FrontEndManager()
        {
            LoadSettings();

            if (_settings.PinballXExists())
            {
                _pinballXManager = new PinballXManager(_settings.PinballXIniPath);
                _pinballXSystems = _pinballXManager.ParseSystems();
            }
        }

        public List<FrontEnd> GetListOfActiveFrontEnds()
        {
            var list = new List<FrontEnd>();
            if (_settings.PinupPopperExists())
                list.Add(new FrontEnd() { Name = FrontEndSystem.PinupPopper.GetDescriptionAttr(), SettingFilePath = _settings.PinupPopperSqlLiteDbPath, System = FrontEndSystem.PinupPopper });
            if (_settings.PinballXExists())
                list.Add(new FrontEnd() { Name = FrontEndSystem.PinballX.GetDescriptionAttr(), SettingFilePath = _settings.PinballXIniPath, System = FrontEndSystem.PinballX });
            if (_settings.PinballYExists())
                list.Add(new FrontEnd() { Name = FrontEndSystem.PinballY.GetDescriptionAttr(), SettingFilePath = _settings.PinballYSettingsPath, System = FrontEndSystem.PinballY });
            return list;
        }

        public List<string> GetFrontEndWarnings(FrontEnd frontEnd)
        {
            var warnings = new List<string>();
            if (frontEnd.System == FrontEndSystem.PinballX)
            {
                //Ensure that fuzzy file matching is turned off, if not tell the user to fix it.
                var iniData = _pinballXManager.ParseIni(_settings.PinballXIniPath);
                if (iniData["FileSystem"]["EnableFileMatching"].ToLower() == "true")
                    warnings.Add("File matching is turned on. Launch PinballX Settings and turn off file matching or you may get unpredictable media showing in the front end. This program relies on this setting being turned off for it's media auditing function to work properly.");
            }
            return warnings;
        }

        public List<PinballXSystem> PinballXSystems { get { return _pinballXSystems; } }
        public ProgramSettings Settings { get { return _settings; } }

        public void SaveSettings(ProgramSettings settings)
        {
            _settingManager.SaveSettings(settings);
        }

        public void LoadSettings()
        {
            _settings = _settingManager.LoadSettings();
            if (_settings == null) //Load the defaults and save to the filesystem
            {
                _settings = new ProgramSettings();
                _settingManager.SaveSettings(_settings);
            }
        }

        public void SaveGame(FrontEndGameViewModel game, string oldGameName)
        {
            if (game.FrontEnd.System == FrontEndSystem.PinballX)
            {
                var system = _pinballXSystems.FirstOrDefault(c => c.DatabaseFiles.Contains(game.DatabaseFile));
                PinballXGame existingGame = null;
                if (!string.IsNullOrEmpty(oldGameName))
                    existingGame = system.Games[game.DatabaseFile].FirstOrDefault(c => c.FileName == oldGameName);
                else
                    existingGame = system.Games[game.DatabaseFile].FirstOrDefault(c => c.FileName == game.FileName);

                PinballXGame pbxGame = null;
                if (existingGame != null)
                    pbxGame = existingGame;
                else
                    pbxGame = new PinballXGame();

                pbxGame = MapViewToGame(system, pbxGame, game);

                _pinballXManager.AddOrUpdateGame(system, game.DatabaseFile, pbxGame);
                SaveDatabase(FrontEndSystem.PinballX, game.DatabaseFile);
            }
        }

        public void DeleteGame(FrontEndGameViewModel game)
        {
            if (game.FrontEnd.System == FrontEndSystem.PinballX)
            {
                var system = _pinballXSystems.FirstOrDefault(c => c.DatabaseFiles.Contains(game.DatabaseFile));
                var existingGame = system.Games[game.DatabaseFile].FirstOrDefault(c => c.FileName == game.FileName);
                system.Games[game.DatabaseFile].Remove(existingGame);
            }
        }

        public void SaveDatabase(FrontEndSystem frontEnd, string databaseFile)
        {
            if (frontEnd == FrontEndSystem.PinballX)
            {
                var system = _pinballXSystems.FirstOrDefault(c => c.DatabaseFiles.Any(b => b.EndsWith(databaseFile)));
                _pinballXManager.SaveDatabase(system, databaseFile, true);
            }
        }

        private PinballXGame MapViewToGame(PinballXSystem system, PinballXGame pbxGame, FrontEndGameViewModel game)
        {
            pbxGame.AlternateExe = game.AlternateExe;
            pbxGame.Author = game.Author;
            pbxGame.Comment = game.Comment;
            pbxGame.DatabaseFile = game.DatabaseFile;
            pbxGame.DateAdded = game.DateAdded.ToString("yyyy-MM-dd HH:mm:ss"); //yyyy-MM-dd HH:mm:ss
            pbxGame.DateModified = game.DateModified.ToString("yyyy-MM-dd HH:mm:ss");
            pbxGame.Description = game.Description;
            pbxGame.Enabled = game.Enabled.ToString();
            pbxGame.FileName = game.FileName;
            pbxGame.HideBackglass = game.HideBackglass.ToString();
            pbxGame.HideDmd = game.HideDmd.ToString();
            pbxGame.HideTopper = game.HideTopper.ToString();
            pbxGame.IPDBNumber = game.IPDBNumber;
            pbxGame.Manufacturer = game.Manufacturer;
            pbxGame.Players = game.Players;
            pbxGame.Rating = game.Rating;
            pbxGame.Rom = game.Rom;
            pbxGame.System = system;
            pbxGame.TableFileUrl = game.TableFileUrl;
            pbxGame.Theme = game.Theme;
            pbxGame.Type = game.Type;
            pbxGame.Version = game.Version;
            pbxGame.Year = game.Year;
            return pbxGame;
        }

        public PinballXSystem GetPinballXSystemByDatabaseFile(string databaseFileName)
        {
            if (_pinballXSystems != null)
            {
                return _pinballXSystems.FirstOrDefault(c => c.DatabaseFiles.Any(d => d == databaseFileName));
            }
            return null;
        }

        public List<FrontEndGameViewModel> GetGamesForFrontEndAndDatabase(FrontEnd frontEnd, string databaseFile)
        {
            var frontEndGames = new List<FrontEndGameViewModel>();
            if (frontEnd != null && !string.IsNullOrEmpty(databaseFile))
            {
                if (frontEnd.System == FrontEndSystem.PinballX)
                    frontEndGames = GetPinballXFrontEndGames(frontEnd, databaseFile);
            }
            return frontEndGames;
        }

        public List<MediaAuditResult> GetMediaAuditResults(FrontEnd frontEnd)
        {
            var auditResults = new List<MediaAuditResult>();
            if (frontEnd != null)
            {
                if (frontEnd.System == FrontEndSystem.PinballX)
                {
                    var games = new List<FrontEndGameViewModel>();
                    foreach (var system in _pinballXSystems)
                    {
                        foreach (var database in system.DatabaseFiles)
                            games.AddRange(GetPinballXFrontEndGames(frontEnd, database));
                    }
                    //Now that we have all the games loaded and we have the statuses on all the media we can check into stranded media
                    //by parsing all the media folders and getting every file inside of it, and matching it up with the games.MediaItems files
                    //and if we have files that don't exist in the games.MediaItems list it is considered a stranded media item
                    List<string> allGameMedia = new List<string>();
                    foreach (var game in games)
                        allGameMedia.AddRange(game.MediaItems.Select(c => c.MediaFullPath));

                    var allMediaFiles = GetAllMediaItems(frontEnd);

                    foreach (var media in allMediaFiles)
                    {
                        if (!allGameMedia.Contains(media.MediaFullPath))
                            auditResults.Add(new MediaAuditResult() { FrontEnd = frontEnd, FullPathToFile = media.MediaFullPath, Status = MediaAuditStatus.UnusedMedia, MediaType = media.MediaType });
                    }
                }
            }
            return auditResults;
        }

        public List<MediaItem> GetAllMediaItems(FrontEnd frontEnd)
        {
            var mediaItems = new List<MediaItem>();

            if (frontEnd.System == FrontEndSystem.PinballX)
            {
                foreach (var system in _pinballXSystems)
                {
                    Log.Information("GetAllMediaItems: Processing PinballX System: {name}. Media Path: {path}", system.Name, system.MediaPath);
                    var rootMediaPath = system.MediaPath.Replace(system.Name, string.Empty);
                    string wheelPath = system.MediaPath + "\\Wheel Images";
                    string flyerPath = rootMediaPath + "Flyer Images";
                    string instructionCardpath = rootMediaPath + "Instruction Cards";
                    string backglassImagePath = system.MediaPath + "\\Backglass Images";
                    string backglassVideoPath = system.MediaPath + "\\Backglass Videos";
                    string dmdImagePath = system.MediaPath + "\\DMD Images";
                    string dmdVideoPath = system.MediaPath + "\\DMD Videos";
                    string launchAudioPath = system.MediaPath + "\\Launch Audio";
                    string realDmdColorImagePath = system.MediaPath + "\\Real DMD Color Images";
                    string realDmdColorVideoPath = system.MediaPath + "\\Real DMD Color Videos";
                    string realDmdImagePath = system.MediaPath + "\\Real DMD Images";
                    string realDmdVideoPath = system.MediaPath + "\\Real DMD Videos";
                    string tableAudioPath = system.MediaPath + "\\Table Audio";
                    string tableImagePath = system.MediaPath + "\\Table Images";
                    string tableVideoPath = system.MediaPath + "\\Table Videos";
                    string tableImageDesktopPath = system.MediaPath + "\\Table Images Desktop";
                    string tableVideoDesktopPath = system.MediaPath + "\\Table Videos Desktop";
                    string topperImagePath = system.MediaPath + "\\Topper Images";
                    string topperVideoPath = system.MediaPath + "\\Topper Videos";

                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Wheel, wheelPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Flyer, flyerPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.InstructionCard, instructionCardpath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Backglass, backglassImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Backglass, backglassVideoPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.DMD, dmdImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.DMD, dmdVideoPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Launch, launchAudioPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.RealDmdColor, realDmdColorImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.RealDmdColor, realDmdColorVideoPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.RealDmd, realDmdImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.RealDmd, realDmdVideoPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Table, tableAudioPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Table, tableImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Table, tableVideoPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.TableDesktop, tableImageDesktopPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.TableDesktop, tableVideoDesktopPath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Topper, topperImagePath));
                    mediaItems.AddRange(GetMediaItemsInDirectory(MediaCategory.Topper, topperVideoPath));
                }
            }

            return mediaItems;
        }

        private List<MediaItem> GetMediaItemsInDirectory(MediaCategory category, string directory)
        {
            var mediaItems = new List<MediaItem>();

            if (Directory.Exists(directory))
            {
                var filesInDirectory = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                foreach (var filePath in filesInDirectory)
                {
                    var mediaItem = new MediaItem() { Category = category, MediaFullPath = filePath };
                    var mimeType = MimeTypes.MimeTypeMap.GetMimeType(filePath);
                    if (mimeType.Contains("audio"))
                        mediaItem.MediaType = MediaType.Audio;
                    else if (mimeType.Contains("video"))
                        mediaItem.MediaType = MediaType.Video;
                    else if (mimeType.Contains("image"))
                        mediaItem.MediaType = MediaType.Image;
                    mediaItems.Add(mediaItem);
                }
            }

            return mediaItems;
        }

        private List<FrontEndGameViewModel> GetPinballXFrontEndGames(FrontEnd frontEnd, string databaseFile)
        {
            var frontEndGames = new List<FrontEndGameViewModel>();
            foreach (var system in PinballXSystems)
            {
                var fullDatabaseFile = system.DatabaseFiles.FirstOrDefault(p => p.Contains(databaseFile));
                if (!string.IsNullOrEmpty(fullDatabaseFile))
                {
                    var games = system.Games[fullDatabaseFile];
                    foreach (var game in games)
                    {
                        var frontEndGame = new FrontEndGameViewModel();
                        frontEndGame.FileName = game.FileName;
                        frontEndGame.FrontEnd = frontEnd;
                        frontEndGame.DatabaseFile = game.DatabaseFile;
                        RefreshGameModel(frontEndGame, system);
                        frontEndGames.Add(frontEndGame);
                    }
                }
            }
            return frontEndGames;
        }

        public void RefreshGameModel(FrontEndGameViewModel frontEndGame, PinballXSystem system)
        {
            var games = system.Games[frontEndGame.DatabaseFile];
            var game = games.FirstOrDefault(c => c.FileName == frontEndGame.FileName);
            frontEndGame.AlternateExe = game.AlternateExe;
            frontEndGame.Author = game.Author;
            frontEndGame.Comment = game.Comment;
            frontEndGame.DatabaseFile = game.DatabaseFile;
            frontEndGame.DateAdded = Convert.ToDateTime(game.DateAdded);
            frontEndGame.DateModified = Convert.ToDateTime(game.DateModified);
            frontEndGame.Description = game.Description;
            frontEndGame.Enabled = Convert.ToBoolean(game.Enabled);
            frontEndGame.FileName = game.FileName;
            //frontEndGame.FrontEnd = frontEnd;
            frontEndGame.HasUpdatesAvailable = false;
            frontEndGame.HideBackglass = Convert.ToBoolean(game.HideBackglass);
            frontEndGame.HideDmd = Convert.ToBoolean(game.HideDmd);
            frontEndGame.HideTopper = Convert.ToBoolean(game.HideTopper);
            frontEndGame.IPDBNumber = game.IPDBNumber;
            frontEndGame.Manufacturer = game.Manufacturer;
            frontEndGame.Players = game.Players;
            frontEndGame.Rating = game.Rating;
            frontEndGame.Rom = game.Rom;
            frontEndGame.Theme = game.Theme;
            frontEndGame.Type = game.Type;
            frontEndGame.Year = game.Year;
            frontEndGame.Version = game.Version;
            frontEndGame.PopperGameId = null;
            frontEndGame.TableFileUrl = game.TableFileUrl;
            frontEndGame.PlatformType = system.Type;

            //Grab the table Statistics
            var stats = _pinballXManager.GetStatisticsData(system.StatsSectionName, game.FileName);
            if (stats != null)
            {
                frontEndGame.SecondsPlayed = stats.SecondsPlayed;
                frontEndGame.TimesPlayed = stats.TimesPlayed;
                frontEndGame.Favorite = stats.Favorite;
            }

            //See if there are discrepencies in the entries, such as missing DirectB2S for .vpx / .vpt files
            //Or missing table 
            LoadPinballXAdditionalInfoAndDiscrepencies(system, frontEndGame);

            //Load the Media Statuses for this game
            LoadPinballXMediaStatus(system, frontEndGame, new SearchMode[] { SearchMode.ByFileNameExactMatch });

            //TODO: cross reference DateAdded with last updated date from game database and flip the HasUpdatesAvailable flag

        }

        private void LoadPinballXAdditionalInfoAndDiscrepencies(PinballXSystem system, FrontEndGameViewModel model)
        {
            model.MissingTable = false;
            //Find the detected game type
            if (system.Type == Platform.VP)
            {
                //Populate the full file path to the game
                if (File.Exists($"{system.TablePath}\\{model.FileName}.vpt"))
                {
                    model.FullPathToTable = $"{system.TablePath}\\{model.FileName}.vpt";
                }
                else if (File.Exists($"{system.TablePath}\\{model.FileName}.vpx"))
                {
                    model.FullPathToTable = $"{system.TablePath}\\{model.FileName}.vpx";
                }

                else
                {
                    //If it's a VPT or VPX, see if the directb2s is found and populate the file path to it as well
                    if (File.Exists($"{system.TablePath}\\{model.FileName}.directb2s"))
                    {
                        model.FullPathToB2s = $"{system.TablePath}\\{model.FileName}.directb2s";
                    }
                }
                //If the file doesn't exist on the file system skip all these checks and mark it as a missing table entry
                if (string.IsNullOrEmpty(model.FullPathToTable))
                    model.MissingTable = true;
            }
            else if (system.Type == Platform.FP) //Could be a .fpt future pinball file
            {
                //Populate the full file path to the game
                if (File.Exists($"{system.TablePath}\\{model.FileName}.fpt"))
                {
                    model.FullPathToTable = $"{system.TablePath}\\{model.FileName}.fpt";
                }
                //If the file doesn't exist on the file system skip all these checks and mark it as a missing table entry
                if (string.IsNullOrEmpty(model.FullPathToTable))
                    model.MissingTable = true;
            }
        }

        private void LoadPinballXMediaStatus(PinballXSystem system, FrontEndGameViewModel model, SearchMode[] searchModes)
        {
            model.MediaItems.Clear();
            var rootMediaPath = system.MediaPath.Replace(system.Name, string.Empty);

            List<string> wheelImages = new List<string>();
            List<string> flyers = new List<string>();
            List<string> instructionCards = new List<string>();
            List<string> manufacturerLogos = new List<string>();
            List<string> apronImages = new List<string>(); //For pinballx these are really the same things as instruction cards
            List<string> backglassImages = new List<string>();
            List<string> backglassVideos = new List<string>();
            List<string> dmdImages = new List<string>();
            List<string> dmdVideos = new List<string>();
            List<string> launchAudios = new List<string>();
            List<string> realDmdColorImages = new List<string>();
            List<string> realDmdColorVideos = new List<string>();
            List<string> realDmdImages = new List<string>();
            List<string> realDmdVideos = new List<string>();
            List<string> tableAudios = new List<string>();
            List<string> tableImages = new List<string>();
            List<string> tableVideos = new List<string>();
            List<string> tableDesktopImages = new List<string>();
            List<string> tableDesktopVideos = new List<string>();
            List<string> topperImages = new List<string>();
            List<string> topperVideos = new List<string>();

            string wheelPath = system.MediaPath + "\\Wheel Images";
            string flyerPath = rootMediaPath + "Flyer Images";
            string instructionCardPath = rootMediaPath + "Instruction Cards";
            string manufacturerLogosPath = rootMediaPath + "Company Logos";
            string apronPath = rootMediaPath + "Instruction Cards"; //Pinball X uses the instruction cards folder for aprons
            string backglassImagePath = system.MediaPath + "\\Backglass Images";
            string backglassVideoPath = system.MediaPath + "\\Backglass Videos";
            string dmdImagePath = system.MediaPath + "\\DMD Images";
            string dmdVideoPath = system.MediaPath + "\\DMD Videos";
            string launchAudioPath = system.MediaPath + "\\Launch Audio";
            string realDmdColorImagePath = system.MediaPath + "\\Real DMD Color Images";
            string realDmdColorVideoPath = system.MediaPath + "\\Real DMD Color Videos";
            string realDmdImagePath = system.MediaPath + "\\Real DMD Images";
            string realDmdVideoPath = system.MediaPath + "\\Real DMD Videos";
            string tableAudioPath = system.MediaPath + "\\Table Audio";
            string tableImagePath = system.MediaPath + "\\Table Images";
            string tableVideoPath = system.MediaPath + "\\Table Videos";
            string tableImageDesktopPath = system.MediaPath + "\\Table Images Desktop";
            string tableVideoDesktopPath = system.MediaPath + "\\Table Videos Desktop";
            string topperImagePath = system.MediaPath + "\\Topper Images";
            string topperVideoPath = system.MediaPath + "\\Topper Videos";

            if (!string.IsNullOrEmpty(model.Manufacturer))
                manufacturerLogos.AddRange(GetMedia(manufacturerLogosPath, model.Manufacturer, false));

            if (searchModes.Contains(SearchMode.ByFileNameExactMatch))
            {
                wheelImages.AddRange(GetMedia(wheelPath, model.FileName));
                flyers.AddRange(GetMedia(flyerPath, model.FileName, true));
                instructionCards.AddRange(GetMedia(instructionCardPath, model.FileName, true));
                apronImages.AddRange(GetMedia(apronPath, model.FileName, true));
                backglassImages.AddRange(GetMedia(backglassImagePath, model.FileName));
                backglassVideos.AddRange(GetMedia(backglassVideoPath, model.FileName));
                dmdImages.AddRange(GetMedia(dmdImagePath, model.FileName));
                dmdVideos.AddRange(GetMedia(dmdVideoPath, model.FileName));
                launchAudios.AddRange(GetMedia(launchAudioPath, model.FileName));
                realDmdColorImages.AddRange(GetMedia(realDmdColorImagePath, model.FileName));
                realDmdColorVideos.AddRange(GetMedia(realDmdColorVideoPath, model.FileName));
                realDmdImages.AddRange(GetMedia(realDmdImagePath, model.FileName));
                realDmdVideos.AddRange(GetMedia(realDmdVideoPath, model.FileName));
                tableAudios.AddRange(GetMedia(tableAudioPath, model.FileName));
                tableImages.AddRange(GetMedia(tableImagePath, model.FileName));
                tableVideos.AddRange(GetMedia(tableVideoPath, model.FileName));
                tableDesktopImages.AddRange(GetMedia(tableImageDesktopPath, model.FileName));
                tableDesktopVideos.AddRange(GetMedia(tableVideoDesktopPath, model.FileName));
                topperImages.AddRange(GetMedia(topperImagePath, model.FileName));
                topperVideos.AddRange(GetMedia(topperVideoPath, model.FileName));
            }
            if (searchModes.Contains(SearchMode.ByDescriptionExactMatch))
            {
                wheelImages.AddRange(GetMedia(wheelPath, model.Description));
                flyers.AddRange(GetMedia(flyerPath, model.Description, true));
                instructionCards.AddRange(GetMedia(instructionCardPath, model.Description, true));
                apronImages.AddRange(GetMedia(apronPath, model.Description, true));
                backglassImages.AddRange(GetMedia(backglassImagePath, model.Description));
                backglassVideos.AddRange(GetMedia(backglassVideoPath, model.Description));
                dmdImages.AddRange(GetMedia(dmdImagePath, model.Description));
                dmdVideos.AddRange(GetMedia(dmdVideoPath, model.Description));
                launchAudios.AddRange(GetMedia(launchAudioPath, model.Description));
                realDmdColorImages.AddRange(GetMedia(realDmdColorImagePath, model.Description));
                realDmdColorVideos.AddRange(GetMedia(realDmdColorVideoPath, model.Description));
                realDmdImages.AddRange(GetMedia(realDmdImagePath, model.Description));
                realDmdVideos.AddRange(GetMedia(realDmdVideoPath, model.Description));
                tableAudios.AddRange(GetMedia(tableAudioPath, model.Description));
                tableImages.AddRange(GetMedia(tableImagePath, model.Description));
                tableVideos.AddRange(GetMedia(tableVideoPath, model.Description));
                tableDesktopImages.AddRange(GetMedia(tableImageDesktopPath, model.Description));
                tableDesktopVideos.AddRange(GetMedia(tableVideoDesktopPath, model.Description));
                topperImages.AddRange(GetMedia(topperImagePath, model.Description));
                topperVideos.AddRange(GetMedia(topperVideoPath, model.Description));
            }

            if (wheelImages.Count() > 0)
            {
                model.HasWheelImage = true;
                model.MediaItems.AddRange(wheelImages.Select(c => new MediaItem() { Category = MediaCategory.Wheel, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            else
                model.HasWheelImage = false;

            if (flyers.Count() > 0)
            {
                model.HasFlyer = true;
                model.MediaItems.AddRange(flyers.Select(c => new MediaItem() { Category = MediaCategory.Flyer, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            else
                model.HasFlyer = false;

            if (instructionCards.Count() > 0)
            {
                model.HasInstructionCard = true;
                model.MediaItems.AddRange(instructionCards.Select(c => new MediaItem() { Category = MediaCategory.InstructionCard, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            else
                model.HasInstructionCard = false;

            if (apronImages.Count() > 0)
            {
                model.ApronMediaStatus = MediaStatus.Image;
                model.MediaItems.AddRange(apronImages.Select(c => new MediaItem() { Category = MediaCategory.Apron, MediaFullPath = c, MediaType = MediaType.Image }));
            }

            if (apronImages.Count() == 0) // && backglassImages.Count() == 0)
                model.ApronMediaStatus = MediaStatus.NotFound;

            if (manufacturerLogos.Count() > 0)
            {
                model.ManufacturerMediaStatus = MediaStatus.Image;
                model.MediaItems.AddRange(manufacturerLogos.Select(c => new MediaItem() { Category = MediaCategory.Manufacturer, MediaFullPath = c, MediaType = MediaType.Image }));
            }
                
            if (manufacturerLogos.Count() == 0) // && backglassImages.Count() == 0)
                model.ManufacturerMediaStatus = MediaStatus.NotFound;

            if (backglassImages.Count() > 0)
            {
                model.BackglassStatus = MediaStatus.Image;
                model.MediaItems.AddRange(backglassImages.Select(c => new MediaItem() { Category = MediaCategory.Backglass, MediaFullPath = c, MediaType = MediaType.Image }));
            }

            if (backglassVideos.Count() > 0)
            {
                model.BackglassStatus = model.BackglassStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(backglassVideos.Select(c => new MediaItem() { Category = MediaCategory.Backglass, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (backglassVideos.Count() == 0 && backglassImages.Count() == 0)
                model.BackglassStatus = MediaStatus.NotFound;

            if (dmdImages.Count() > 0)
            {
                model.DMDStatus = MediaStatus.Image;
                model.MediaItems.AddRange(dmdImages.Select(c => new MediaItem() { Category = MediaCategory.DMD, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (dmdVideos.Count() > 0)
            {
                model.DMDStatus = model.DMDStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(dmdVideos.Select(c => new MediaItem() { Category = MediaCategory.DMD, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (dmdVideos.Count() == 0 && dmdImages.Count() == 0)
                model.DMDStatus = MediaStatus.NotFound;

            if (launchAudios.Count() > 0)
            {
                model.HasLaunchAudio = true;
                model.MediaItems.AddRange(launchAudios.Select(c => new MediaItem() { Category = MediaCategory.Launch, MediaFullPath = c, MediaType = MediaType.Audio }));
            }
            else
                model.HasLaunchAudio = false;

            if (realDmdColorImages.Count() > 0)
            {
                model.RealDMDColorStatus = MediaStatus.Image;
                model.MediaItems.AddRange(realDmdColorImages.Select(c => new MediaItem() { Category = MediaCategory.RealDmdColor, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (realDmdColorVideos.Count() > 0)
            {
                model.RealDMDColorStatus = model.RealDMDColorStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(realDmdColorVideos.Select(c => new MediaItem() { Category = MediaCategory.RealDmdColor, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (realDmdColorVideos.Count() == 0 && realDmdColorImages.Count() == 0)
                model.RealDMDColorStatus = MediaStatus.NotFound;

            if (realDmdImages.Count() > 0)
            {
                model.ReadDMDStatus = MediaStatus.Image;
                model.MediaItems.AddRange(realDmdImages.Select(c => new MediaItem() { Category = MediaCategory.RealDmd, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (realDmdVideos.Count() > 0)
            {
                model.ReadDMDStatus = model.ReadDMDStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(realDmdVideos.Select(c => new MediaItem() { Category = MediaCategory.RealDmd, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (realDmdVideos.Count() == 0 && realDmdImages.Count() == 0)
                model.ReadDMDStatus = MediaStatus.NotFound;

            if (launchAudios.Count() > 0)
            {
                model.HasTableAudio = true;
                model.MediaItems.AddRange(launchAudios.Select(c => new MediaItem() { Category = MediaCategory.Launch, MediaFullPath = c, MediaType = MediaType.Audio }));
            }
            else
                model.HasTableAudio = false;

            if (tableImages.Count() > 0)
            {
                model.TableStatus = MediaStatus.Image;
                model.MediaItems.AddRange(tableImages.Select(c => new MediaItem() { Category = MediaCategory.Table, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (tableVideos.Count() > 0)
            {
                model.TableStatus = model.TableStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(tableVideos.Select(c => new MediaItem() { Category = MediaCategory.Table, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (tableVideos.Count() == 0 && tableImages.Count() == 0)
                model.TableStatus = MediaStatus.NotFound;

            if (tableDesktopImages.Count() > 0)
            {
                model.TableDesktopStatus = MediaStatus.Image;
                model.MediaItems.AddRange(tableDesktopImages.Select(c => new MediaItem() { Category = MediaCategory.TableDesktop, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (tableDesktopVideos.Count() > 0)
            {
                model.TableDesktopStatus = model.TableDesktopStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(tableDesktopVideos.Select(c => new MediaItem() { Category = MediaCategory.TableDesktop, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (tableDesktopVideos.Count() == 0 && tableDesktopImages.Count() == 0)
                model.TableDesktopStatus = MediaStatus.NotFound;

            if (topperImages.Count() > 0)
            {
                model.TopperStatus = MediaStatus.Image;
                model.MediaItems.AddRange(topperImages.Select(c => new MediaItem() { Category = MediaCategory.Topper, MediaFullPath = c, MediaType = MediaType.Image }));
            }
            if (topperVideos.Count() > 0)
            {
                model.TopperStatus = model.TopperStatus == MediaStatus.Image ? MediaStatus.ImageAndVideo : MediaStatus.Video;
                model.MediaItems.AddRange(topperVideos.Select(c => new MediaItem() { Category = MediaCategory.Topper, MediaFullPath = c, MediaType = MediaType.Video }));
            }

            if (topperVideos.Count() == 0 && topperImages.Count() == 0)
                model.TopperStatus = MediaStatus.NotFound;
        }

        private List<string> GetMedia(string searchPath, string fileNameSearchText, bool searchSubDirectorys = false)
        {
            List<string> mediaFilesFound = new List<string>();
            if (Directory.Exists(searchPath))
            {
                string[] files = null;
                if (searchSubDirectorys)
                    files = Directory.GetFiles(searchPath, fileNameSearchText + "*", SearchOption.AllDirectories);
                else
                    files = Directory.GetFiles(searchPath, fileNameSearchText + "*", SearchOption.TopDirectoryOnly);
                mediaFilesFound.AddRange(files.Where(p => Path.GetFileNameWithoutExtension(p) == fileNameSearchText));
            }
            return mediaFilesFound;
        }

        public ToolResult LaunchGame(FrontEndGameViewModel game, LaunchType launchType)
        {
            var result = new ToolResult();
            if (game.FrontEnd.System == FrontEndSystem.PinballX)
            {
                Log.Information("{ToolName}: Pinball X System Launch command initiated. Launch Type: {type}", ToolName, launchType);
                var system = _pinballXSystems.FirstOrDefault(c => c.DatabaseFiles.Contains(game.DatabaseFile));
                var existingGame = system.Games[game.DatabaseFile].FirstOrDefault(c => c.FileName == game.FileName);

                //Construct the full process launch command, taking into account if this game has an alternate .exe defined in the database
                string launchCommand = string.Empty;
                string args = string.Empty;
                if (system.Type == Platform.VP || system.Type == Platform.FP)
                {
                    //The Visual Pinball parameters. The following tags are supported and will be replaced with values - [TABLEPATH], [TABLEFILE], [TABLEFILEWOEXT], [MANUFACTURER], [YEAR], [SYSTEM], [RATING], [DESCRIPTION].
                    launchCommand = system.WorkingPath;
                    if (!string.IsNullOrEmpty(existingGame.AlternateExe))
                        launchCommand += "\\" + existingGame.AlternateExe;
                    else
                        launchCommand += "\\" + system.Executable;

                    if (launchType == LaunchType.LaunchGame)
                    {
                        args += system.Parameters;
                    }
                    else if (launchType == LaunchType.LaunchGameInConfigMode && system.Type == Platform.VP) //put into table edit mode so we can send the F6 key to enter camera / light mode, no command line option for this :(
                    {
                        if (!string.IsNullOrEmpty(game.FullPathToTable))
                        {
                            var fileInfo = new FileInfo(game.FullPathToTable);
                            if (fileInfo.Extension.Contains("vpt"))
                            {
                                return new ToolResult(ToolName, new ValidationResult()
                                {
                                    IsValid = false,
                                    Messages = new List<ValidationMessage>() { new ValidationMessage("Launch game in configuration mode not valid for VPT files as the older versions of visual pinball didn't have that feature.", MessageLevel.Error) }
                                });
                            }
                        }
                        args += system.Parameters.Replace("-play", "-edit").Replace("/play", "/edit").Replace("\\play", "\\edit");
                    }
                    else if (launchType == LaunchType.LaunchGameInConfigMode && system.Type == Platform.FP) //put into table edit mode so we can send the F6 key to enter camera / light mode, no command line option for this :(
                    {
                        //args += system.Parameters.Replace("-play", "-open").Replace("/play", "/open").Replace("\\play", "\\open");
                        //Remove the /play and /exit commands that are normally present so it only opens to the editor
                        args += system.Parameters.Replace("/play", "").Replace("/exit", "").Replace("\\play", "").Replace("\\exit", "");
                    }
                    else if (launchType == LaunchType.LaunchGameUsingFrontEnd)
                    {
                        launchCommand = system.PinballXFolder + "\\PinballX.exe";
                        args = "-launch \"[TABLEFILEWOEXT]\" \"[SYSTEM]\"";
                    }
                }
                else //PinballFX, Custom, PinballArcade
                {
                    //The Pinball FX2 or Steam parameters. Use default for Steam. The following tags are supported and will be replaced with values - [TABLEPATH], [TABLEFILE], [TABLEFILEWOEXT], [MANUFACTURER], [YEAR], [SYSTEM], [RATING], [DESCRIPTION].
                    //The Pinball Arcade or Steam parameters. Use default for Steam.The following tags are supported and will be replaced with values - [TABLEPATH], [TABLEFILE], [TABLEFILEWOEXT], [MANUFACTURER], [YEAR], [SYSTEM], [RATING], [DESCRIPTION].
                    //The Other System parameters. The following tags are supported and will be replaced with values - [TABLEPATH], [TABLEFILE], [TABLEFILEWOEXT], [MANUFACTURER], [YEAR], [SYSTEM], [RATING], [DESCRIPTION].
                    if (launchType == LaunchType.LaunchGameInConfigMode)
                    {
                        Log.Error("{toolname}: Launch game in configuration mode not valid for game type.", ToolName);
                        return new ToolResult(ToolName, new ValidationResult()
                        {
                            IsValid = false,
                            Messages = new List<ValidationMessage>() { new ValidationMessage("Launch game in configuration mode not valid for game type.", MessageLevel.Error) }
                        });
                    }
                    launchCommand = system.WorkingPath;
                    if (!string.IsNullOrEmpty(existingGame.AlternateExe))
                        launchCommand += "\\" + existingGame.AlternateExe;
                    else
                        launchCommand += "\\" + system.Executable;

                    if (launchType == LaunchType.LaunchGame)
                    {
                        args += system.Parameters;
                    }
                    else if (launchType == LaunchType.LaunchGameUsingFrontEnd)
                    {
                        launchCommand = system.PinballXFolder + "\\PinballX.exe";
                        args = "-launch \"[TABLEFILEWOEXT]\" \"[SYSTEM]\"";
                    }
                }

                //Replace the launch tokens
                if (!string.IsNullOrEmpty(game.FullPathToTable))
                {
                    var fileInfo = new FileInfo(game.FullPathToTable);
                    args = args.Replace("[TABLEFILE]", fileInfo.Name);
                }
                else //Not a path to a physical table, could be a partial fill in for a steam launch command for pinball fx3 for example
                    args = args.Replace("[TABLEFILE]", existingGame.FileName);

                if (system.TablePath != null)
                    args = args.Replace("[TABLEPATH]", system.TablePath);

                args = args.Replace("[TABLEFILEWOEXT]", existingGame.FileName);
                if (!string.IsNullOrEmpty(existingGame.Manufacturer))
                    args = args.Replace("[MANUFACTURER]", existingGame.Manufacturer);
                if (!string.IsNullOrEmpty(existingGame.Year))
                    args = args.Replace("[YEAR]", existingGame.Year);
                if (!string.IsNullOrEmpty(existingGame?.System?.Name))
                    args = args.Replace("[SYSTEM]", existingGame.System.Name);
                args = args.Replace("[RATING]", existingGame.Rating.ToString());
                if (!string.IsNullOrEmpty(existingGame.Description))
                    args = args.Replace("[DESCRIPTION]", existingGame.Description);

                var startInfo = new ProcessStartInfo(launchCommand, args);
                startInfo.WorkingDirectory = system.WorkingPath;

                //Check if UAC is enabled and pass in runas verb
                if (RegistryUtil.CheckIfUacEnabled())
                    startInfo.Verb = "runas";

                Log.Information("{ToolName}: Starting Process: {process}, args: {args}, working directory: {workingdir}", ToolName, launchCommand, args, system.WorkingPath);
                var process = Process.Start(startInfo);

                if (launchType == LaunchType.LaunchGameInConfigMode && system.Type == Platform.VP)
                {
                    Log.Information("{ToolName}: Sending F6 key");
                    process.WaitForInputIdle();
                    WinApi.SetForegroundWindow(process.MainWindowHandle);
                    SendKeys.SendWait("{F6}");
                    Log.Information("{ToolName}: Finished sending F6 key", ToolName);
                }

            }
            //Launch type of using front end is probably not applicable for PinballY and PinupPopper as I don't think they have any documented
            //command line switches. TODO: Check PinballY source code, Pinup Popper is closed source and doesn't appear to have any command line options
            return result;
        }
    }
}
