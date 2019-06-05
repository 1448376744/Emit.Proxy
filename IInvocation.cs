using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Emit.Proxy
{
    public interface IInvocation
    {
        object[] Arguments { get; set; }
        object ReturnValue { get; set; }
        object Target { get; }
        MethodInfo TargetMethod { get; }
        Type TargetType { get; }
        void Proceed();
    }
    public class DefaultInvocation : IInvocation
    {
        public object[] Arguments { get; set; }
        public object Target { get; set; }
        public MethodInfo TargetMethod { get; set; }
        public object ReturnValue { get; set; }
        public Type TargetType { get; set; }
        public void Proceed()
        {
            ReturnValue = TargetMethod.Invoke(Target, Arguments);
        }
    }

}
