using System;
using System.Collections.Generic;
using System.Text;

namespace Emit.Proxy
{
    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }
}
