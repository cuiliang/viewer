﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;

namespace Viewer.UI.UserSettings
{
    internal interface ISettingsView : IWindowView
    {
        /// <summary>
        /// Event occurs whenever the <see cref="Programs"/> collection is changed by the user
        /// </summary>
        event EventHandler ProgramsChanged;

        /// <summary>
        /// List of external applications which can be run with files as their arguments
        /// </summary>
        List<ExternalApplication> Programs { get; set; }
    }
}
