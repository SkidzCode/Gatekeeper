using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace GateKeeper.Server.Inherites
{
    public class MyControllerBase : ControllerBase
    {
        protected static string FunctionName([CallerMemberName] string functionName = "")
        {
            return functionName;
        }

        
    }
}
