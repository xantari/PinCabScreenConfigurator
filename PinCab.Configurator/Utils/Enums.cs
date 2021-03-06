﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinCab.Configurator.Utils
{
    public enum BackgroundProgressAction
    {
        PinMameWriteAllPreviousRunRoms,
        PinMameValidateAllPreviousRunRoms,
        PinMameWriteRowDataToAllPreviousRunRoms,
        PinMameWriteCellDataToAllPreviousRunRoms
    }

    public enum DatabaseManagerBackgroundProgressAction
    {
        DownloadDatabases,
        ProcessDatabase,
        DownloadAndLoadDatabase,
        LoadTags,
        RunAllQueuedTasks
    }
}
