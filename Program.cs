using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWcfTest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EndpointAddress = CoreWCF.EndpointAddress;
using SecurityMode = CoreWCF.SecurityMode;

namespace CoreWcfTest;


[DataContract]
public class EchoFault
{
    [DataMember]
    [AllowNull]
    public string Text { get; set; }
}

[System.ServiceModel.ServiceContract]
public interface IEchoService
{
    [System.ServiceModel.OperationContract]
    string Echo(string text);
    
    [System.ServiceModel.OperationContract]
    string ComplexEcho(EchoMessage text);
    
    [System.ServiceModel.OperationContract]
    [CoreWCF.FaultContract(typeof(EchoFault))]
    string FailEcho(string text);

}

[DataContract]
public class EchoMessage
{
    [AllowNull]
    [DataMember]
    public string Text { get; set; }
}

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class EchoService : IEchoService
{
    private IHttpContextAccessor HttpContextAccessor;

    public EchoService(IHttpContextAccessor httpContextAccessor)
    {
        this.HttpContextAccessor = httpContextAccessor;
    }

    public string Echo(string text)
    {
        Console.WriteLine("Server Echo" + HttpContextAccessor?.HttpContext?.Request.Path.ToString());
        return text;
    }

    public string ComplexEcho(EchoMessage text)
    {
        Console.WriteLine("Server Complex Echo");
        return text.Text;
    }

    public string FailEcho(string text)
    {
        throw new NotImplementedException();
    }
}


internal class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.AllowSynchronousIO = true;
        });
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // Add WSDL support
        builder.Services.AddServiceModelServices().AddServiceModelMetadata();
        builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
        builder.Services.AddSingleton<EchoService>();

        var app = builder.Build();
        //app.Services.GetService(typeof(EchoService));


        // Configure an explicit none credential type for WSHttpBinding as it defaults to Windows which requires extra configuration in ASP.NET
        var myWSHttpBinding = new CoreWCF.WSHttpBinding(SecurityMode.Transport);
        myWSHttpBinding.Security.Transport.ClientCredentialType = CoreWCF.HttpClientCredentialType.None;


        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        app.UseServiceModel(builder =>
        {
            builder.AddService<EchoService>((serviceOptions) => { })
                // Add a BasicHttpBinding at a specific endpoint
                .AddServiceEndpoint<EchoService, IEchoService>(new CoreWCF.BasicHttpBinding(), "/EchoService/basichttp")
                // Add a WSHttpBinding with Transport Security for TLS
                .AddServiceEndpoint<EchoService, IEchoService>(myWSHttpBinding, "/EchoService/WSHttps");
        });


        var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
        serviceMetadataBehavior.HttpGetEnabled = true;

        Task.Run(() => app.Run());

        Thread.Sleep(500);


        var binding = new System.ServiceModel.BasicHttpBinding
        {
            MaxReceivedMessageSize = Int32.MaxValue,
            ReaderQuotas = XmlDictionaryReaderQuotas.Max,
            Security = new System.ServiceModel.BasicHttpSecurity()
        };


        var endpointAddress = new System.ServiceModel.EndpointAddress(new Uri("http://localhost:5000/EchoService/basichttp"));
        var channelFactory = new ChannelFactory<IEchoService>(binding, endpointAddress);

        var proxy = channelFactory.CreateChannel();

        Console.WriteLine("Reply: " + proxy.Echo("HELLO"));

        Console.Read();
    }
}