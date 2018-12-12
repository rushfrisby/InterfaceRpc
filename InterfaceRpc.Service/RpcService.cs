using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InterfaceRpc.Service
{
	public class RpcService<T> where T : class //would be nice to have "where T : interface" here
	{
		private readonly HttpListener _listener = new HttpListener();
		private const string PlainTextContentType = "text/plain";
		private readonly RouteManager<T> _routeManager;
		private CancellationTokenSource _cancellationTokenSource;
		private Task _listenTask;
		private readonly RpcSettings _settings;

		public RpcService(T implementation, RpcSettings settings = null)
		{
			if(!typeof(T).IsInterface)
			{
				throw new ApplicationException($"{typeof(T).Name} is not an interface. The type of T in RpcService<T> must be an interface.");
			}
			if (!HttpListener.IsSupported)
			{
				throw new PlatformNotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
			}

			_settings = settings ?? RpcSettings.Load();

			if (_settings.WebServerPrefixes == null || !_settings.WebServerPrefixes.Any())
			{
				throw new ApplicationException("No web server prefixes found in settings.");
			}

			_routeManager = new RouteManager<T>(implementation);

			foreach (var s in _settings.WebServerPrefixes)
			{
				_listener.Prefixes.Add(s);
			}

			_listener.IgnoreWriteExceptions = true;
		}

		public void Start()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_listener.Start();

			_listenTask = Task.Run(async () =>
			{
				var sem = new Semaphore(_settings.MaxConnections, _settings.MaxConnections);

				while (_listener.IsListening)
				{
					sem.WaitOne();
					try
					{
						await _listener.GetContextAsync().ContinueWith(async (t) =>
						{
							sem.Release();
							var context = await t;
							Exception handlerException = null;
							try
							{
								await HandleRequest(context);
							}
							catch (Exception ex)
							{
								handlerException = ex;
							}
							if (handlerException != null)
							{
								await WriteInternalServerErrorAsync(handlerException.Message, context);
							}
							if (context != null && context.Response.OutputStream != null)
							{
								context.Response.OutputStream.Close();
							}
						});
					}
					catch (Exception ex)
					{
						//connection aborted?
						//TODO: log ex
					}
				}

				sem.Dispose();

			}, _cancellationTokenSource.Token);

			
		}

		private async Task HandleRequest(HttpListenerContext context)
		{
			if (context == null)
			{
				return;
			}

			if (!_routeManager.ContainsHandler(context.Request.RawUrl))
			{
				var notFound = Encoding.UTF8.GetBytes("Not Found");
				context.Response.StatusCode = (int)HttpStatusCode.NotFound;
				context.Response.StatusDescription = "Not Found";
				context.Response.ContentType = PlainTextContentType;
				context.Response.ContentLength64 = notFound.Length;
				await context.Response.OutputStream.WriteAsync(notFound, 0, notFound.Length);
				return;
			}

			var requestHandler = _routeManager.GetHandler(context.Request.RawUrl);
			RouteResponse response = null;
			Exception routeException = null;
			try
			{
				response = await requestHandler(context);
			}
			catch (Exception ex)
			{
				routeException = ex;
			}

			if (routeException != null)
			{
				//TODO: Log routeException
				await WriteInternalServerErrorAsync(routeException.Message, context);
				return;
			}
			if (response == null)
			{
				//TODO: Log response
				await WriteInternalServerErrorAsync("Route provided no response.", context);
				return;
			}

			context.Response.StatusCode = (int)HttpStatusCode.OK;
			context.Response.StatusDescription = "OK";
			context.Response.ContentType = response.ContentType;
			var contentLength = response.Content == null ? 0 : response.Content.Length;
			context.Response.ContentLength64 = contentLength;
			if (contentLength > 0)
			{
				await context.Response.OutputStream.WriteAsync(response.Content, 0, contentLength);
			}
		}

		private async static Task WriteInternalServerErrorAsync(string message, HttpListenerContext context)
		{
			if (context == null || context.Response.OutputStream == null)
			{
				return;
			}
			if (String.IsNullOrWhiteSpace(message))
			{
				message = "Internal Server Error";
			}
			var data = Encoding.UTF8.GetBytes(message);
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			context.Response.StatusDescription = "Internal Server Error";
			context.Response.ContentType = PlainTextContentType;
			context.Response.ContentLength64 = data.Length;
			await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
		}

		public void Stop()
		{
			if (_listener != null)
			{
				if (_listener.IsListening)
				{
					_listener.Stop();
				}
				if (_listenTask != null)
				{
					while (!_listenTask.IsCompleted)
					{
						//give it some time to finish up
						Thread.Sleep(1000 * 3);
					}
					if(!_listenTask.IsCompleted)
					{
						_cancellationTokenSource.Cancel();
						//give it some more time to finish up
						Thread.Sleep(1000 * 3);
					}
					_listenTask.Dispose();
				}
				_listener.Close();
			}
		}
	}
}
