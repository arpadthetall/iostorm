using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public class InvokeContext
    {
        public string OriginDeviceId { get; internal set; }

        //public Payload.IPayload Request { get; internal set; }

        public IObserver<Payload.IPayload> Response { get; private set; }

        public InvokeContext(/*Payload.IPayload request,*/ IObserver<Payload.IPayload> response)
        {
//            Request = request;
            Response = response;
        }

        //public static object CreateGenericFromPayload(InvokeContext incoming, Payload.IPayload payload)
        //{
        //    var genericType = typeof(InvokeContext<>).MakeGenericType(payload.GetType());

        //    var invCtx = (InvokeContext)Activator.CreateInstance(genericType, payload, incoming.Response);

        //    invCtx.OriginDeviceId = incoming.OriginDeviceId;

        //    return invCtx;
        //}
    }

    //public class InvokeContext<T> : InvokeContext where T : class, Payload.IPayload
    //{
    //    public InvokeContext(T request, IObserver<Payload.IPayload> response)
    //        : base(request, response)
    //    {
    //    }

    //    public new T Request
    //    {
    //        get
    //        {
    //            return (base.Request as T);
    //        }
    //    }
    //}
}
