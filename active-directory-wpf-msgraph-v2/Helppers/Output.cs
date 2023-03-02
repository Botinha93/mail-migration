using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace active_directory_wpf_msgraph_v2.Helppers
{
    class Output
    {
        static public IList<Output> registry { get; set; } = new List<Output>();
        public String Code { get; set; }
        public String Description { get; set; }
        public Output(String code, String Description)
        {
            this.Description = Description;
            this.Code = code;
            registry.Add(this);
            MainWindow.RefreshListview();
        }
    }
}
