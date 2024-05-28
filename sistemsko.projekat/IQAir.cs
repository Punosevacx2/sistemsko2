using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjekatSP
{
    public class IQAir
    {
        public int data { get; set; }
        public string status { get; set; }

        public IQAir(string status)
        {
            this.status = status;
        }
    }
   
}
