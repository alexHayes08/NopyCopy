﻿using System;

namespace NopyCopyV2.Modals
{
    public class NopCommerceSolutionEvent : EventArgs
    {
        public bool SolutionLoaded { get; set; }
        public string SolutionName { get; set; }
    }
}
