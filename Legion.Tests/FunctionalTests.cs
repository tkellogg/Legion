using System.Collections.Specialized;
using System.Net;
using Moq;
using NUnit.Framework;

namespace Legion.Tests
{
    public class FunctionalTests
    {
        [Test]
        public void It_receives_requests_on_registered_paths()
        {
            using (var host = new Host())
            {
                var handlerMock = StartService(host, "http://localhost:8090/test_legion/");

                var web = new WebClient();
                var str = web.DownloadString("http://localhost:8090/test_legion/");
                Assert.That(str, Is.Empty);
								handlerMock.Verify(x => x.Handle(It.IsAny<HttpListenerRequest>(), It.IsAny<HttpListenerResponse>()));
            }
        }

				[Test]
				public void It_receives_404_when_not_handled()
				{
            using (var host = new Host())
            {
                var handlerMock = StartService(host, "http://localhost:8090/test_legion/");

                var web = new WebClient();
                var ex = Assert.Throws<WebException>(() => web.DownloadString("http://localhost:8090/test_legion2/"));
								handlerMock.Verify(x => x.Handle(It.IsAny<HttpListenerRequest>(), It.IsAny<HttpListenerResponse>()), Times.Never());

                var resp = ex.Response as HttpWebResponse;
								Assert.That((int) resp.StatusCode, Is.EqualTo(404));
            }
				}

        private static Mock<IHttpHandler> StartService(Host host, string uri)
        {
            var handlerMock = new Mock<IHttpHandler>();
            handlerMock.Setup(x => x.ListenUrl).Returns(uri);
            host.AddHandler(handlerMock.Object);
            host.Start();
            return handlerMock;
        }
    }
}
