using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Net.WebSockets;
using System.Collections.Generic;

/////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
// Basic almost-single-file Http-Server library that I can drop into projects for easy use
// Somewhat based on NodeJS's Koa/DenoJS's Oak. 
// Simple control with a stack-like middleware-oriented arrangement.
// Pass `MiddleWare` functions into `HttpServer.Watch` or `HttpServer.Serve`
namespace MiniHttp { 

	/// <summary> Primary workhorse of this framework </summary>
	/// <param name="context"> <see cref="Ctx"/> object representing the request/response and everything else </param>
	/// <param name="next"> <see cref="NextFn"/> which calls the next middleware in the stack. </param>
	/// <returns> Async <see cref="Task"/> object </returns>
	public delegate Task Middleware(Ctx context, NextFn next);
	/// <summary> Delegate of 'next' for clarity and convinience. Call one of these to call the next middleware in the stack. </summary>
	/// <returns> Async <see cref="Task"/> object </returns>
	public delegate Task NextFn();
	/// <summary> Primary context object, mostly wraps <see cref="HttpListenerContext"/>, and adds some things for convinience </summary>
	public class Ctx {
		/// <summary> Embedded <see cref="Req"/> object </summary>
		public Req req { get; private set; }
		/// <summary> Embedded <see cref="Res"/> object </summary>
		public Res res { get; private set; }
		/// <summary> Raw, wrapped <see cref="HttpListenerContext"/> object </summary>
		public HttpListenerContext raw { get; private set; }

		/// <summary> JsonObject holding query parameters </summary>
		public JsonObject query { get; private set; }
		/// <summary> JsonObject holding URL derived parameters </summary>
		public JsonObject param { get; private set; }
		/// <summary> JsonObject for custom middleware to use. </summary>
		public JsonObject midData { get; private set; }

		/// <summary> Requested path split on '/' chars </summary>
		public string[] pathSplit { get { return req.pathSplit; } }

		/// <summary> Default constructor, used when beginning handling of a request. </summary>
		/// <param name="context"> Raw <see cref="HttpListenerContext"/> to wrap </param>
		public Ctx(HttpListenerContext context) {
			raw = context;
			req = new Req(this, context.Request);
			res = new Res(this, context.Response);
			KeepAlive = true;

			query = req.QueryString.ToJsonObject();
			param = new JsonObject();
			midData = new JsonObject();
		
		}
		#region HttpListenerContext wrappers
		/// <summary> Gets the <see cref="HttpListenerContext.User"/>. </summary>
		public IPrincipal User { get { return raw.User; } }
		/// <summary> Wraps <see cref="HttpListenerContext.AcceptWebSocketAsync(string)"/></summary>
		public Task<HttpListenerWebSocketContext> AcceptWegbSocketAsync(string subProtocol) { 
			return raw.AcceptWebSocketAsync(subProtocol); 
		}
		/// <summary> Wraps <see cref="HttpListenerContext.AcceptWebSocketAsync(string, int, TimeSpan)"/></summary>
		public Task<HttpListenerWebSocketContext> AcceptWegbSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval) { 
			return raw.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval); 
		}
		/// <summary> Wraps <see cref="HttpListenerContext.AcceptWebSocketAsync(string, int, TimeSpan, ArraySegment{byte})"/></summary>
		public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer) { 
			return raw.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer); 
		}
		/// <summary> Wraps <see cref="HttpListenerContext.AcceptWebSocketAsync(string,  TimeSpan)"/></summary>
		public Task<HttpListenerWebSocketContext> AcceptWegbSocketAsync(string subProtocol,  TimeSpan keepAliveInterval) { 
			return raw.AcceptWebSocketAsync(subProtocol, keepAliveInterval); 
		}
		#endregion
		#region HttpListenerRequest wrappers
		/// <summary> Was this request originally HTTPS? (SSL) Wraps into <see cref="HttpListenerRequest.IsSecureConnection"/> </summary>
		public bool IsSecureConnection { get { return raw.Request.IsSecureConnection; } }
		/// <summary> Was this a Websocket request? Wraps into <see cref="HttpListenerRequest.IsWebSocketRequest"/> </summary>
		public bool IsWebSocketRequest { get { return raw.Request.IsWebSocketRequest; } }
		/// <summary> Is the client authenticated? Wraps into <see cref="HttpListenerRequest.IsAuthenticated"/> </summary>
		public bool IsAuthenticated { get { return raw.Request.IsAuthenticated; } }
		/// <summary> Does this request contain data? Wraps into <see cref="HttpListenerRequest.HasEntityBody"/> </summary>
		public bool HasEntityBody { get { return raw.Request.HasEntityBody; } }
		/// <summary> Was this request sent by the same computer? (loopback) Wraps into <see cref="HttpListenerRequest.IsLocal"/> </summary>
		public bool IsLocal { get { return raw.Request.IsLocal; } }
		/// <summary> Gets the error code, if any, for problems with the client's certificate. Wraps into <see cref="HttpListenerRequest.ClientCertificateError"/> </summary>
		public int ClientCertificateError { get { return raw.Request.ClientCertificateError; } }
		/// <summary> Gets the string representation of the client's request Wraps into <see cref="HttpListenerRequest.UserHostAddress"/> </summary>
		public string UserHostAddress { get { return raw.Request.UserHostAddress; } }
		/// <summary> Gets the `User-Agent` header, if provided. Wraps into <see cref="HttpListenerRequest.UserAgent"/> </summary>
		public string UserAgent { get { return raw.Request.UserAgent; } }
		/// <summary> Gets the service provider name. Wraps into <see cref="HttpListenerRequest.ServiceName"/> </summary>
		public string ServiceName { get { return raw.Request.ServiceName; } }
		/// <summary> Gets the URL information (Without host and port). Wraps into <see cref="HttpListenerRequest.RawUrl"/> </summary>
		public string RawUrl { get { return raw.Request.RawUrl; } }
		/// <summary> Gets the host and port. Wraps into <see cref="HttpListenerRequest.UserHostName"/> </summary>
		public string UserHostName { get { return raw.Request.UserHostName; } }
		/// <summary> Gets the requested HTTP method. Wraps into <see cref="HttpListenerRequest.HttpMethod"/> </summary>
		public string HttpMethod { get { return raw.Request.HttpMethod; } }
		/// <summary> Gets the request's MIME type. Wraps into <see cref="HttpListenerRequest.ContentType"/> </summary>
		public string ContentType { get { return raw.Request.ContentType; } }
		/// <summary> Gets the requested languages. Wraps into <see cref="HttpListenerRequest.UserLanguages"/> </summary>
		public string[] UserLanguages { get { return raw.Request.UserLanguages; } }
		/// <summary> Gets the acceptable response types. Wraps into <see cref="HttpListenerRequest.AcceptTypes"/> </summary>
		public string[] AcceptTypes { get { return raw.Request.AcceptTypes; } }
		/// <summary> Gets the `Referrer` header. Wraps into <see cref="HttpListenerRequest.UrlReferrer"/> </summary>
		public Uri UrlReferrer { get { return raw.Request.UrlReferrer; } }
		/// <summary> Gets the URI object requested by the client. Wraps into <see cref="HttpListenerRequest.Url"/> </summary>
		public Uri Url { get { return raw.Request.Url; } }
		/// <summary> Gets a stream containing the body data . Wraps into <see cref="HttpListenerRequest.InputStream"/> </summary>
		public Stream InputStream { get { return raw.Request.InputStream; } }
		/// <summary> Gets the TransportContext of the request. Wraps into <see cref="HttpListenerRequest.TransportContext"/> </summary>
		public TransportContext TransportContext { get { return raw.Request.TransportContext; } }
		/// <summary> Gets the Cookies in the request. Wraps into <see cref="HttpListenerRequest.Cookies"/> </summary>
		public CookieCollection Cookies { get { return raw.Request.Cookies; } }
		/// <summary> Gets the remote end point. Wraps into <see cref="HttpListenerRequest.RemoteEndPoint"/> </summary>
		public IPEndPoint RemoteEndPoint { get { return raw.Request.RemoteEndPoint; } }
		/// <summary> Gets the local end point. Wraps into <see cref="HttpListenerRequest.LocalEndPoint"/> </summary>
		public IPEndPoint LocalEndPoint { get { return raw.Request.LocalEndPoint; } }
		/// <summary> Gets the Request trace ID. Wraps <see cref="HttpListenerRequest.RequestTraceIdentifier"/> </summary>
		public Guid RequestTraceIdentifier { get { return raw.Request.RequestTraceIdentifier; } }
		#endregion
		#region HttpListenerResponse wrappers
		/// <summary> Gets/sets the HTTP status code. Wraps into <see cref="HttpListenerResponse.StatusCode"/>. </summary>
		public int StatusCode { get { return raw.Response.StatusCode; } set { raw.Response.StatusCode = value; } }
		/// <summary> Gets/sets the HTTP status description. Wraps into <see cref="HttpListenerResponse.StatusDescription"/>. </summary>
		public string StatusDescription { get { return raw.Response.StatusDescription; } set { raw.Response.StatusDescription = value; } }
		/// <summary> Gets/sets if chunked transmission should be used. Wraps into <see cref="HttpListenerResponse.SendChunked"/>. </summary>
		public bool SendChunked { get { return raw.Response.SendChunked; } set { raw.Response.SendChunked = value; } }
		/// <summary> Gets/sets if the connection should be kept open. Wraps into <see cref="HttpListenerResponse.KeepAlive"/>. </summary>
		public bool KeepAlive { get { return raw.Response.KeepAlive; } set { raw.Response.KeepAlive = value; } }
		/// <summary> Gets/sets the redirect location. Wraps into <see cref="HttpListenerResponse.RedirectLocation"/>. </summary>
		public string RedirectLocation { get { return raw.Response.RedirectLocation; } set { raw.Response.RedirectLocation = value; } }
		/// <summary> Gets/sets the protocol version. Wraps into <see cref="HttpListenerResponse.ProtocolVersion"/>. </summary>
		public Version ProtocolVersion { get { return raw.Response.ProtocolVersion; } set { raw.Response.ProtocolVersion = value; } }
		/// <summary> Gets/sets the encoding to use. Wraps into <see cref="HttpListenerResponse.ContentEncoding"/>. </summary>
		public Encoding ContentEncoding { get { return raw.Response.ContentEncoding; } set { raw.Response.ContentEncoding = value; } }
		/// <summary> Gets the output stream. Wraps into <see cref="HttpListenerResponse.OutputStream"/>. </summary>
		public Stream OutputStream { get { return raw.Response.OutputStream; } }
		#endregion
	

		public override string ToString() {
			return $"{RemoteEndPoint} => {LocalEndPoint} | {HttpMethod} @ {UserHostName}{RawUrl}\n{HttpServerHelpers.FmtPath(pathSplit)}";
		}

		/// <summary> Convinient accessor for <see cref="Res.body"/>. </summary>
		public object body { get { return res.body; } set { res.body = value; } }


	}

	/// <summary> Request object, mostly wraps <see cref="HttpListenerRequest"/>, and adds some things for convinience </summary>
	public class Req {
		/// <summary> Parent <see cref="Ctx"/> object. </summary>
		public Ctx ctx { get; private set; }
		/// <summary> Raw, wrapped <see cref="HttpListenerRequest"/> object </summary>
		public HttpListenerRequest raw { get; private set; }
		/// <summary> Constructor, used by <see cref="Ctx.Ctx(HttpListenerContext)"/></summary>
		/// <param name="owner"> Owner <see cref="Ctx"/> object </param>
		/// <param name="context"> Raw <see cref="HttpListenerRequest"/> to wrap </param>
		public Req(Ctx owner, HttpListenerRequest request) {
			ctx = owner;
			raw = request;

			pathSplit = HttpServerHelpers.UpToFirst(request.RawUrl, "?").Split("/");

		}
		#region HttpListenerRequest wrappers
		/// <summary> Was this request originally HTTPS? (SSL) Wraps into <see cref="HttpListenerRequest.IsSecureConnection"/> </summary>
		public bool IsSecureConnection { get { return raw.IsSecureConnection; } }
		/// <summary> Should this connection be kept alive? Wraps into <see cref="HttpListenerRequest.KeepAlive"/> </summary>
		public bool KeepAlive { get { return raw.KeepAlive; } }
		/// <summary> Was this a Websocket request? Wraps into <see cref="HttpListenerRequest.IsWebSocketRequest"/> </summary>
		public bool IsWebSocketRequest { get { return raw.IsWebSocketRequest; } }
		/// <summary> Is the client authenticated? Wraps into <see cref="HttpListenerRequest.IsAuthenticated"/> </summary>
		public bool IsAuthenticated { get { return raw.IsAuthenticated; } }
		/// <summary> Does this request contain data? Wraps into <see cref="HttpListenerRequest.HasEntityBody"/> </summary>
		public bool HasEntityBody { get { return raw.HasEntityBody; } }
		/// <summary> Was this request sent by the same computer? (loopback) Wraps into <see cref="HttpListenerRequest.IsLocal"/> </summary>
		public bool IsLocal { get { return raw.IsLocal; } }
		/// <summary> Gets the error code, if any, for problems with the client's certificate. Wraps into <see cref="HttpListenerRequest.ClientCertificateError"/> </summary>
		public int ClientCertificateError { get { return raw.ClientCertificateError; } }
		/// <summary> Get the length of data included Wraps into <see cref="HttpListenerRequest.ContentLength64"/> </summary>
		public long ContentLength64 { get { return raw.ContentLength64; } }
		/// <summary> Gets the string representation of the client's request Wraps into <see cref="HttpListenerRequest.UserHostAddress"/> </summary>
		public string UserHostAddress { get { return raw.UserHostAddress; } }
		/// <summary> Gets the `User-Agent` header, if provided. Wraps into <see cref="HttpListenerRequest.UserAgent"/> </summary>
		public string UserAgent { get { return raw.UserAgent; } }
		/// <summary> Gets the service provider name. Wraps into <see cref="HttpListenerRequest.ServiceName"/> </summary>
		public string ServiceName { get { return raw.ServiceName; } }
		/// <summary> Gets the URL information (Without host and port). Wraps into <see cref="HttpListenerRequest.RawUrl"/> </summary>
		public string RawUrl { get { return raw.RawUrl; } }
		/// <summary> Gets the host and port. Wraps into <see cref="HttpListenerRequest.UserHostName"/> </summary>
		public string UserHostName { get { return raw.UserHostName; } }
		/// <summary> Gets the requested HTTP method. Wraps into <see cref="HttpListenerRequest.HttpMethod"/> </summary>
		public string HttpMethod { get { return raw.HttpMethod; } }
		/// <summary> Gets the request's MIME type. Wraps into <see cref="HttpListenerRequest.ContentType"/> </summary>
		public string ContentType { get { return raw.ContentType; } }
		/// <summary> Gets the requested languages. Wraps into <see cref="HttpListenerRequest.UserLanguages"/> </summary>
		public string[] UserLanguages { get { return raw.UserLanguages; } }
		/// <summary> Gets the acceptable response types. Wraps into <see cref="HttpListenerRequest.AcceptTypes"/> </summary>
		public string[] AcceptTypes { get { return raw.AcceptTypes; } }
		/// <summary> Gets the `Referrer` header. Wraps into <see cref="HttpListenerRequest.UrlReferrer"/> </summary>
		public Uri UrlReferrer { get { return raw.UrlReferrer; } }
		/// <summary> Gets the URI object requested by the client. Wraps into <see cref="HttpListenerRequest.Url"/> </summary>
		public Uri Url { get { return raw.Url; } }
		/// <summary> Gets the HTTP version of the request Wraps into <see cref="HttpListenerRequest.ProtocolVersion"/> </summary>
		public Version ProtocolVersion { get { return raw.ProtocolVersion; } }
		/// <summary> Gets a stream containing the body data . Wraps into <see cref="HttpListenerRequest.InputStream"/> </summary>
		public Stream InputStream { get { return raw.InputStream; } }
		/// <summary> Gets the Encoding object of the request, if available. Wraps into <see cref="HttpListenerRequest.ContentEncoding"/> </summary>
		public Encoding ContentEncoding { get { return raw.ContentEncoding; } }
		/// <summary> Gets the TransportContext of the request. Wraps into <see cref="HttpListenerRequest.TransportContext"/> </summary>
		public TransportContext TransportContext { get { return raw.TransportContext; } }
		/// <summary> Gets the headers of the request. Wraps into <see cref="HttpListenerRequest.Headers"/> </summary>
		public NameValueCollection Headers { get { return raw.Headers; } }
		/// <summary> Gets the QueryString of the request. Wraps into <see cref="HttpListenerRequest.QueryString"/> </summary>
		public NameValueCollection QueryString { get { return raw.QueryString; } }
		/// <summary> Gets the Cookies in the request. Wraps into <see cref="HttpListenerRequest.Cookies"/> </summary>
		public CookieCollection Cookies { get { return raw.Cookies; } }
		/// <summary> Gets the remote end point. Wraps into <see cref="HttpListenerRequest.RemoteEndPoint"/> </summary>
		public IPEndPoint RemoteEndPoint { get { return raw.RemoteEndPoint; } }
		/// <summary> Gets the local end point. Wraps into <see cref="HttpListenerRequest.LocalEndPoint"/> </summary>
		public IPEndPoint LocalEndPoint { get { return raw.LocalEndPoint; } }
		/// <summary> Gets the Request trace ID. Wraps <see cref="HttpListenerRequest.RequestTraceIdentifier"/> </summary>
		public Guid RequestTraceIdentifier { get { return raw.RequestTraceIdentifier; } }

		/// <summary> Wraps <see cref="HttpListenerRequest.BeginGetClientCertificate(AsyncCallback, object)"/> </summary>
		public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state) { return raw.BeginGetClientCertificate(requestCallback, state); }
		/// <summary> Wraps <see cref="HttpListenerRequest.EndGetClientCertificate(IAsyncResult)"/> </summary>
		public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult) { return raw.EndGetClientCertificate(asyncResult); }
		/// <summary> Wraps <see cref="HttpListenerRequest.GetClientCertificate"/> </summary>
		public X509Certificate2 GetClientCertificate() { return raw.GetClientCertificate(); }
		/// <summary> Wraps <see cref="HttpListenerRequest.GetClientCertificateAsync"/> </summary>
		public Task<X509Certificate2> GetClientCertificateAsync() { return raw.GetClientCertificateAsync(); }
		#endregion
		/// <summary> Accesses the entire request body as a <see cref="byte[]"/>. Populated by the <see cref="ProvidedMiddleware.BodyParser"/> </summary>
		public byte[] data { get; set; }
		/// <summary> Available to hold the request's string version, if present. Populated by the <see cref="ProvidedMiddleware.BodyParser"/> </summary>
		public string body { get; set; }
		/// <summary> <see cref="JsonObject"/> parsed from body, if successful. Populated by the <see cref="ProvidedMiddleware.BodyParser"/> </summary>
		public JsonObject bodyObj { get; set; }
		/// <summary> <see cref="JsonArray"/> parsed from body, if successful. Populated by the <see cref="ProvidedMiddleware.BodyParser"/> </summary>
		public JsonArray bodyArr { get; set; }

		/// <summary> Requested path split on '/' chars </summary>
		public string[] pathSplit { get; private set; }
	}
	/// <summary> Response object, mostly wraps <see cref="HttpListenerResponse"/>, and adds some things for convinience </summary>
	public class Res {
		private static readonly Version VERSION = new Version("1.1");
		/// <summary> Parent <see cref="Ctx"/> object. </summary>
		public Ctx ctx { get; private set; }
		/// <summary> Raw, wrapped <see cref="HttpListenerResponse"/> object </summary>
		public HttpListenerResponse raw { get; private set; }
		/// <summary> Constructor, used by <see cref="Ctx.Ctx(HttpListenerContext)"/></summary>
		/// <param name="owner"> Owner <see cref="Ctx"/> object </param>
		/// <param name="context"> Raw <see cref="HttpListenerResponse"/> to wrap </param>
		public Res(Ctx owner, HttpListenerResponse response) {
			ctx = owner;
			raw = response;
			ContentEncoding ??= Encoding.UTF8;
			StatusCode = 404;
			StatusDescription = "404 Not Found";
			ProtocolVersion = VERSION;
			SendChunked = false;

			body = "404 Not Found";
		}
		#region HttpListenerResponse wrappers
		/// <summary> Gets/sets the Content-Length header. Wraps into <see cref="HttpListenerResponse.ContentLength64"/>. </summary>
		public long ContentLength64 { get { return raw.ContentLength64; } set { raw.ContentLength64 = value; } }
		/// <summary> Gets/sets the HTTP status code. Wraps into <see cref="HttpListenerResponse.StatusCode"/>. </summary>
		public int StatusCode { get { return raw.StatusCode; } set { raw.StatusCode = value; } }
		/// <summary> Gets/sets the HTTP status description. Wraps into <see cref="HttpListenerResponse.StatusDescription"/>. </summary>
		public string StatusDescription { get { return raw.StatusDescription; } set { raw.StatusDescription = value; } }
		/// <summary> Gets/sets if chunked transmission should be used. Wraps into <see cref="HttpListenerResponse.SendChunked"/>. </summary>
		public bool SendChunked { get { return raw.SendChunked; } set { raw.SendChunked = value; } }
		/// <summary> Gets/sets if the connection should be kept open. Wraps into <see cref="HttpListenerResponse.KeepAlive"/>. </summary>
		public bool KeepAlive { get { return raw.KeepAlive; } set { raw.KeepAlive = value; } }
		/// <summary> Gets/sets the content-type. Wraps into <see cref="HttpListenerResponse.ContentType"/>. </summary>
		public string ContentType { get { return raw.ContentType; } set { raw.ContentType = value; } }
		/// <summary> Gets/sets the redirect location. Wraps into <see cref="HttpListenerResponse.RedirectLocation"/>. </summary>
		public string RedirectLocation { get { return raw.RedirectLocation; } set { raw.RedirectLocation = value; } }
		/// <summary> Gets/sets the protocol version. Wraps into <see cref="HttpListenerResponse.ProtocolVersion"/>. </summary>
		public Version ProtocolVersion { get { return raw.ProtocolVersion; } set { raw.ProtocolVersion = value; } }
		/// <summary> Gets/sets the response headers. Wraps into <see cref="HttpListenerResponse.Headers"/>. </summary>
		public WebHeaderCollection Headers { get { return raw.Headers; } set { raw.Headers = value; } }
		/// <summary> Gets/sets the response cookies. Wraps into <see cref="HttpListenerResponse.Cookies"/>. </summary>
		public CookieCollection Cookies { get { return raw.Cookies; } set { raw.Cookies = value; } }
		/// <summary> Gets/sets the encoding to use. Wraps into <see cref="HttpListenerResponse.ContentEncoding"/>. </summary>
		public Encoding ContentEncoding { get { return raw.ContentEncoding; } set { raw.ContentEncoding = value; } }
		/// <summary> Gets the output stream. Wraps into <see cref="HttpListenerResponse.OutputStream"/>. </summary>
		public Stream OutputStream { get { return raw.OutputStream; } }

		/// <summary> Wraps <see cref="HttpListenerResponse.Abort"/>. </summary>
		public void Abort() { raw.Abort(); }
		/// <summary> Wraps <see cref="HttpListenerResponse.AddHeader(string, string)"/>. </summary>
		public void AddHeader(string name, string value) { raw.AddHeader(name, value); }
		/// <summary> Wraps <see cref="HttpListenerResponse.AppendCookie(Cookie)"/>. </summary>
		public void AppendCookie(Cookie cookie) { raw.AppendCookie(cookie); }
		/// <summary> Wraps <see cref="HttpListenerResponse.AppendHeader(string, string)"/>. </summary>
		public void AppendHeader(string name, string value) { raw.AppendHeader(name, value); }
		/// <summary> Wraps <see cref="HttpListenerResponse.Close(byte[], bool)"/>. </summary>
		public void Close(byte[] responseEntity, bool willBlock) { raw.Close(responseEntity, willBlock); }
		/// <summary> Wraps <see cref="HttpListenerResponse.Close"/>. </summary>
		public void Close() { raw.Close(); }
		/// <summary> Wraps <see cref="HttpListenerResponse.CopyFrom(HttpListenerResponse)"/>. </summary>
		public void CopyFrom(HttpListenerResponse templateResponse) { raw.CopyFrom(templateResponse); }
		/// <summary> Wraps <see cref="HttpListenerResponse.Redirect(string)"/>. </summary>
		public void Redirect(string url) { raw.Redirect(url); }
		/// <summary> Wraps <see cref="HttpListenerResponse.SetCookie(Cookie)"/>. </summary>
		public void SetCookie(Cookie cookie) { raw.SetCookie(cookie); }


		#endregion
	
	
		/// <summary> Currently assigned object representing the body. </summary>
		public object body { get; set; }

	}


	/// <summary> Contains entry points standing up http servers. </summary>
	public class HttpServer {
		/// <summary> Continuously retries hosting an HTTP server </summary>
		/// <param name="prefixes"> List of hostnames/ports to accept. </param>
		/// <param name="stayOpen"> Delegate that provides a <see cref="bool"/> value. True while the server should stay open. </param>
		/// <param name="restartDelay"> Millisecond delay between restarts, should the program crash.</param>
		/// <param name="middleware"> Array of middleware to use </param>
		/// <returns> Task that completes when the server stops listening. </returns>
		public static async Task<int> Watch(string[] prefixes, Func<bool> stayOpen, int restartDelay, params Middleware[] middleware) {
			prefixes = prefixes.Select(it=>(it.EndsWith("/") ? it : it+"/")).ToArray();

			int ret = 1;
			while (ret != 0) {
				ret = await Serve(prefixes, stayOpen, middleware);
				await Task.Delay(restartDelay);
			}

			return 0;
		}

		/// <summary> Begins hosting an HTTP server</summary>
		/// <param name="prefixes"> List of hostnames/ports to accept. </param>
		/// <param name="stayOpen"> Delegate that provides a <see cref="bool"/> value. True while the server should stay open. </param>
		/// <param name="middleware"> Array of middleware to use </param>
		/// <returns> Task that completes when the server stops listening. </returns>
		public static async Task<int> Serve(string[] prefixes, Func<bool> stayOpen, params Middleware[] middleware) {
			if (!HttpListener.IsSupported) {
				Console.WriteLine("Cannot start HTTP server, unsupported on this platform.");
				return -2;
			}
		
			if (prefixes == null || prefixes.Length == 0) { return -1; }
			using (HttpListener listener = new HttpListener()) {
				try {
					foreach (var prefix in prefixes) { listener.Prefixes.Add(prefix); }
					listener.Start();
				
					while (stayOpen()) {
						Ctx ctx = new Ctx(await listener.GetContextAsync());
						// Toss into other task and resume listening.
						var _ = Task.Run(()=>Handle(ctx, middleware));
					}

				} catch (Exception e) {
					Console.WriteLine("HttpServer.Serve: Internal Error - " + e);
					return -1;
				}
			}


			return 0;
		}


		/// <summary> Inner function used to handle HTTP contexts. </summary>
		/// <param name="ctx"> Context to handle </param>
		/// <param name="middleware"> Middleware stack to use </param>
		/// <returns> Task that completes when request has been handled. </returns>
		internal static async Task Handle(Ctx ctx, Middleware[] middleware) {
			NextFn next(int i) {
				return async () => {
					if (i+1 >= middleware.Length) { return; }
					await middleware[i+1](ctx, next(i+1));
				};
			}

			try { 
				// Kick everything off
				await next(-1)();

			} catch (Exception e) {
				// Top level exception, srs issue.
				Console.WriteLine($"HttpServer.Handle: Internal error in context: {ctx}\n{e}");
				ctx.StatusCode = 500;
				ctx.StatusDescription = $"Internal Server Error";
				ctx.body = $"500 Internal Server Error {e.GetType()} / {e.Message} \nStack Trace:\n{e.StackTrace}";
			
			}

			byte[] data = ctx.body as byte[];
			if (ctx.body is string) {
				data = ctx.ContentEncoding.GetBytes(ctx.body as string);
			}
			if (data == null) {
				data = ctx.ContentEncoding.GetBytes("");
			}

			var res = ctx.res.raw;
			res.ContentLength64 = data.Length;
			await res.OutputStream.WriteAsync(data, 0, data.Length);

			if (!ctx.KeepAlive) {
				res.OutputStream.Close();
			}

		}

	}


	public class Router {
		public class Route {
			public string method;
			public string pattern;
			public string[] splitPattern;
			public Middleware[] handlers;
			public Route(string method, string pattern, params Middleware[] handlers) {
				if (!pattern.StartsWith('/')) { pattern = '/' + pattern; }
				this.method = method;
				this.pattern = pattern;
				splitPattern = pattern.Split("/");
				this.handlers = handlers;
			}
		}
		private List<Route> routes = new List<Route>();

		public void Use(string pattern, Router router) {
			routes.Add(new Route("*", pattern, router.Routes));
		}
		public void Get(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("GET", pattern, handlers));
		}
		public void Head(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("HEAD", pattern, handlers));
		}
		public void Post(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("POST", pattern, handlers));
		}
		public void Put(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("PUT", pattern, handlers));
		}
		public void Delete(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("DELETE", pattern, handlers));
		}
		public void Options(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("OPTIONS", pattern, handlers));
		}
		public void Connect(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("CONNECT", pattern, handlers));
		}
		public void Trace(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("TRACE", pattern, handlers));
		}
		public void Patch(string pattern, params Middleware[] handlers) {
			routes.Add(new Route("PATCH", pattern, handlers));
		}
		public Middleware Routes { get { return Handle; } }

		private async Task Handle(Ctx ctx, NextFn next) {
			// TODO: Maybe a match scoring system?
			Console.WriteLine($"Matching request path of {HttpServerHelpers.FmtPath(ctx.pathSplit)}");
			foreach (var route in routes) {
				Console.WriteLine($"To Route path  {HttpServerHelpers.FmtPath(route.splitPattern)}");
				if (route.method == "*" || route.method == ctx.HttpMethod) {
					if (PathMatches(ctx, route)) {

					}


				}

			}
		}

		public static bool PathMatches(Ctx ctx, Route route) {

			int n = route.splitPattern.Length;
			for (int i = 0; i < n; i++) {


			}

			return false;
		}

	}

	public static class ProvidedMiddleware {
		public static readonly Middleware BodyParser = async (ctx, next) => {
			byte[] buffer = new byte[2048];
			using (MemoryStream stream = new MemoryStream()) {
				int read = 0;
				int size = 0;
				do {
					read = await ctx.InputStream.ReadAsync(buffer, 0, 2048);
					if (read > 0) {
						size += read;
						await stream.WriteAsync(buffer,0, read);
					}

				} while (read > 0);

				byte[] final = stream.ToArray();
				if (ctx.req.ContentEncoding != null) {
					ctx.req.body = ctx.req.ContentEncoding.GetString(final);
					try {
						JsonValue result = Json.ParseStrict(ctx.req.body);
						if (result is JsonObject) { ctx.req.bodyObj = result as JsonObject; }
						if (result is JsonArray) { ctx.req.bodyArr = result as JsonArray; }
					} catch (Exception) { }

				
				} else {
					ctx.req.data = final;
				}


			}
			await next();
		};
		public static readonly Middleware Inspect = async (ctx, next) => {
			void print(string prefix) {
				Console.WriteLine($"{prefix}: {ctx}");
				Console.WriteLine($"Query: {ctx.query}");
				Console.WriteLine($"Params: {ctx.param}");
				Console.WriteLine($"Raw body: {ctx.req.body}");
				Console.WriteLine($"Object: {ctx.req.bodyObj?.ToString()}");
				Console.WriteLine($"Array: {ctx.req.bodyArr?.ToString()}");
			}
			print("\nBefore");
			await next();
			print("\nAfter");
		};
	}

	public static class HttpServerHelpers {
		public static JsonObject ToJsonObject(this NameValueCollection coll) {
			JsonObject obj = new JsonObject();

			string[] keys = coll.AllKeys;
			for (int i = 0; i < keys.Length; i++) {
				string key = keys[i];
				string value = coll[key];
				// Console.WriteLine($"Pair {i} - \"{key}\"=\"{value}\"");
			
				if (key == null) {
					// Invert this and set them as flags as true...
					// Why would you use null as the KEY?
					string[] values = value.Split(',');
					foreach (var val in values) { obj[val] = true; }
				} else {
					// Otherwise treat singletons as a value, lists as an array.
					if (value.Contains(',')) {
						string[] values = value.Split(',');
						obj[key] = new JsonArray().AddAll(values);
					} else {
						obj[key] = value;
					}

				}
			}

			return obj;
		}

		public static string UpToFirst(string str, string search) {
			if (str.Contains(search)) {
				int ind = str.IndexOf(search);
				return str.Substring(0, ind);
			}
			return str;
		}
		public static string FmtPath(string[] paths) {
			return string.Join(" -> ", paths.Select(it => (it == null || it == "") ? "/" : it));
		}
	}
}