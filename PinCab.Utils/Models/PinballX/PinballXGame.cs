﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PinCab.Utils.Models.PinballX
{
    //Order matters in the XML element's or you'll get a lot of diff errors if you compare what pinballx writes versus what
    //this writes, so we match the order in which PinballX writes them here.
    [XmlRoot("game")]
    public class PinballXGame
    {

        // "official hyperpin" fields
        // ----------------------------------

        /// <summary>
        /// Filename without extension and path.
        /// apart from the latter including extension if file exists.
        /// </summary>
        [XmlAttribute("name")]
        public string FileName { get; set; }

        /// <summary>
        /// The identifier used for media.
        /// </summary>
        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("rom")]
        public string Rom { get; set; }

        [XmlElement("manufacturer")]
        public string Manufacturer { get; set; }

        [XmlElement("year")]
        public string Year { get; set; }

        /// <summary>
        /// SS,EM
        /// </summary>
        [XmlElement("type")]
        public string Type { get; set; }


        // pinballx fields
        // ----------------------------------
        [XmlElement("hidedmd")]
        public string HideDmd { get; set; }

        [XmlElement("hidetopper")]
        public string HideTopper { get; set; }

        [XmlElement("hidebackglass")]
        public string HideBackglass { get; set; }

        [XmlElement("enabled")]
        public string Enabled { get; set; }

        [XmlElement("rating")]
        public double Rating { get; set; }

        [XmlElement("alternateexe")]
        public string AlternateExe { get; set; }

        [XmlElement("SendKeysOnStart")]
        public string SendKeysOnStart { get; set; }
        [XmlElement("SendKeysOnExit")]
        public string SendKeysOnExit { get; set; }

        [XmlElement("players")]
        public string Players { get; set; }
        [XmlElement("comment")]
        public string Comment { get; set; }
        [XmlElement("theme")]
        public string Theme { get; set; }
        [XmlElement("author")]
        public string Author { get; set; }
        [XmlElement("version")]
        public string Version { get; set; }
        [XmlElement("IPDBnr")]
        public string IPDBNumber { get; set; }
        //yyyy-MM-dd HH:mm:ss
        [XmlElement("dateadded")]
        public string DateAdded { get; set; }
        //yyyy-MM-dd HH:mm:ss
        [XmlElement("datemodified")]
        public string DateModified { get; set; }

        /// <summary>
        /// For tracking when updates occur to this game, track the original table URL
        /// </summary>
        [XmlElement("tablefileurl")]
        public string TableFileUrl { get; set; }

        // internal fields (not serialized)
        // ----------------------------------
        [XmlIgnore]
        public string DatabaseFile { get; set; }

        [XmlIgnore]
        public PinballXSystem System { get; set; }

        public void Update(PinballXGame newGame)
        {
            FileName = newGame.FileName;
            Description = newGame.Description;
            Manufacturer = newGame.Manufacturer;
            Year = newGame.Year;
            Type = newGame.Type;
            HideDmd = newGame.HideDmd;
            HideBackglass = newGame.HideBackglass;
            Enabled = newGame.Enabled;
            Rating = newGame.Rating;
            AlternateExe = newGame.AlternateExe;
            SendKeysOnStart = newGame.SendKeysOnStart;
            SendKeysOnExit = newGame.SendKeysOnExit;
            Rom = newGame.Rom;
            Players = newGame.Players;
            Comment = newGame.Comment;
            Theme = newGame.Theme;
            Author = newGame.Author;
            Version = newGame.Version;
            IPDBNumber = newGame.IPDBNumber;
            DateAdded = newGame.DateAdded;
            DateModified = newGame.DateModified;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            var game = obj as PinballXGame;
            if (game == null)
            {
                return false;
            }

            // really, we only care about those.
            return
                FileName == game.FileName &&
                Description == game.Description &&
                Enabled == game.Enabled &&
                AlternateExe == game.AlternateExe;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Description.GetHashCode();
        }
    }
}
