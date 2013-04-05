using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Legion
{
    public class Host : IDisposable
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly ConcurrentQueue<IHttpHandler> handlers = new ConcurrentQueue<IHttpHandler>();
        private Thread listenerThread;
        private volatile int counter;

        #region Starting and stopping

        public void Start()
        {
            listener.Start();
            listenerThread = new Thread(Listen);
            listenerThread.Start();
        }

        ~Host()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (listener.IsListening) listener.Stop();
                if (listenerThread != null)
                {
                    listenerThread.Abort();
                    listenerThread = null;
                }
            }
        }

        #endregion

        public void AddHandler(IHttpHandler handler)
        {
            if (handler == null) return;
            handlers.Enqueue(handler);
            listener.Prefixes.Add(handler.ListenUrl);
        }

        private void Listen()
        {
            while (true)
            {
                HttpListenerContext context = null;
                try
                {
                    context = listener.GetContext();
                    counter++;
                    ThreadPool.QueueUserWorkItem(o => ProcessOne(context));
                }
                catch (Exception e)
                {
					HandleError(context, e);
                }
            }
        }

		void HandleError(HttpListenerContext context, Exception ex)
		{
			if (context != null)
			{
				context.Response.StatusCode = 500;
				var buffer = Encoding.UTF8.GetBytes(ex.ToString());
				context.Response.OutputStream.Write(buffer, 0, buffer.Length);
				context.Response.Close();
			}
			Console.WriteLine(ex);
		}

        private void ProcessOne(HttpListenerContext context)
        {
            HttpListenerResponse resp = null;
            try
            {
                var req = context.Request;
                resp = context.Response;
                var path = context.Request.Url.AbsolutePath;
                var handler = handlers.FirstOrDefault(x => x.Supports(path));
                if (handler == null)
                {
                    HandleNotFound(context);
                    return;
                }

                handler.Handle(req, resp);
            }
            catch (Exception e)
            {
				HandleError(context, e);
                Console.WriteLine(e);
            }
            finally
            {
				if (resp != null) resp.Close();
                counter--;
            }
        }

        private void HandleNotFound(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
        }
    }
}
