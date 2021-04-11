using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Models
{
    public enum VisibilityStatus
    {
        NONVISIBLE,
        VISIBLE
    }

    public static class VisibilityExtensions
    {
        public static bool IsVisible(this VisibilityStatus visibility)
        {
            return visibility == VisibilityStatus.VISIBLE;
        }

        public static VisibilityStatus ToVisibility(this bool visibility)
        {
            switch (visibility)
            {
                case true:
                    return VisibilityStatus.VISIBLE;
                default:
                    return VisibilityStatus.NONVISIBLE;
            }
        }
    }
}
