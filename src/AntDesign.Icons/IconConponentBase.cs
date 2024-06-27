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

        [Parameter]
        public bool Spin { get; set; }

        [Parameter]
        public double Rotate { get; set; }
    }
}
