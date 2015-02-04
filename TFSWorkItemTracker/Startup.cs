using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartup(typeof(TFSWorkItemTracker.Startup))]

namespace TFSWorkItemTracker
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var serializerSettings = new JsonSerializerSettings
            //{
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    NullValueHandling = NullValueHandling.Ignore
            //};
            //GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => JsonSerializer.Create(serializerSettings));

            app.MapSignalR();
            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888
        }
    }
}
