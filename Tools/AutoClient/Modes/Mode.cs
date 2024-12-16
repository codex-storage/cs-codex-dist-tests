using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoClient.Modes
{
    public interface IMode
    {
        void Start(ICodexInstance instance, int index);
        void Stop();
    }
}
