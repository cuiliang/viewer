﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public interface IComponent
    {
        /// <summary>
        /// Function called once when this component should be loaded
        /// </summary>
        /// <param name="app">Application</param>
        void OnStartup(IViewerApplication app);
    }
}