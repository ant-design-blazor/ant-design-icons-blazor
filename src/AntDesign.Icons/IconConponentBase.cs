using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntDesign.Icons
{
    public abstract class IconConponentBase: ComponentBase
    {
        [Parameter]
        public string Class { get; set; } = string.Empty;

        [Parameter]
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// Sets the accessible role of the rendered icon.
        /// </summary>
        [Parameter]
        public string Role { get; set; } = "img";

        /// <summary>
        /// Sets the accessible name of the rendered icon.
        /// </summary>
        [Parameter]
        public string? AriaLabel { get; set; }

        [Parameter]
        public bool Spin { get; set; }

        [Parameter]
        public double Rotate { get; set; }
    }
}
