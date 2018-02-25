using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace TumblThree.Applications.Auth
{
    /// <summary>
    ///     A class to manage OAuth interactions.  This works with
    ///     Twitter, not sure about other OAuth-enabled services.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class holds the relevant oauth parameters, and exposes
    ///         methods that do things, based on those params.
    ///     </para>
    ///     <para>
    ///         See http://dev.twitpic.com/docs/2/upload/ for an example of the
    ///         oauth parameters.  The params include token, consumer_key,
    ///         timestamp, version, and so on.  In the actual HTTP message, they
    ///         all include the oauth_ prefix, so ..  oauth_token,
    ///         oauth_timestamp, and so on.  You set these via a string indexer.
    ///         If the instance of the class is called oauth, then to set
    ///         the oauth_token parameter, you use oath["token"] in C#.
    ///     </para>
    ///     <para>
    ///         This class automatically sets many of the required oauth parameters;
    ///         this includes the timestamp, nonce, callback, and version parameters.
    ///         (The callback param is initialized to 'oob'). You can reset any of
    ///         these parameters as you see fit.  In many cases you won't have to.
    ///     </para>
    ///     <para>
    ///         The public methods on the class include:
    ///         AcquireRequestToken, AcquireAccessToken,
    ///         GenerateCredsHeader, and GenerateAuthorizationHeader.  The
    ///         first two are used only on the first run of an applicaiton,
    ///         or after a user has explicitly de-authorized an application
    ///         for use with OAuth.  Normally, the GenerateXxxHeader methods
    ///         can be used repeatedly, when sending HTTP messages that
    ///         require an OAuth Authorization header.
    ///     </para>
    ///     <para>
    ///         The AcquireRequestToken and AcquireAccessToken methods
    ///         actually send out HTTP messages.
    ///     </para>
    ///     <para>
    ///         The GenerateXxxxHeaders are used when constructing and
    ///         sending your own HTTP messages.
    ///     </para>
    /// </remarks>
    public class OAuthManager
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private static readonly string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private readonly Dictionary<string, string> _params;
        private readonly Random _random;

        /// <summary>
        ///     The default public constructor.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Initializes various fields to default values.
        ///     </para>
        /// </remarks>
        public OAuthManager()
        {
            _random = new Random();
            _params = new Dictionary<string, string>
            {
                ["consumer_key"] = "",
                ["consumer_secret"] = "",
                ["timestamp"] = GenerateTimeStamp(),
                ["nonce"] = GenerateNonce(),
                ["signature_method"] = "HMAC-SHA1",
                ["signature"] = "",
                ["token"] = "",
                ["token_secret"] = "",
                ["version"] = "1.0"
            };
        }

        /// <summary>
        ///     The constructor to use when using OAuth when you already
        ///     have an OAuth access token.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The parameters for this constructor all have the
        ///         meaning you would expect.  The token and tokenSecret
        ///         are set in oauth_token, and oauth_token_secret.
        ///         These are *Access* tokens, obtained after a call
        ///         to AcquireAccessToken.  The application can store
        ///         those tokens and re-use them on successive runs.
        ///         For twitter at least, the access tokens never expire.
        ///     </para>
        /// </remarks>
        public OAuthManager(string consumerKey,
            string consumerSecret,
            string token,
            string tokenSecret) : this()

        {
            _params["consumer_key"] = consumerKey;
            _params["consumer_secret"] = consumerSecret;
            _params["token"] = token;
            _params["token_secret"] = tokenSecret;
        }

        /// <summary>
        ///     string indexer to get or set oauth parameter values.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use the parameter name *without* the oauth_ prefix.
        ///         If you want to set the value for the oauth_token parameter
        ///         field in an HTTP message, then use oauth["token"].
        ///     </para>
        ///     <para>
        ///         The set of oauth param names known by this indexer includes:
        ///         callback, consumer_key, consumer_secret, timestamp, nonce,
        ///         signature_method, signature, token, token_secret, and version.
        ///     </para>
        ///     <para>
        ///         If you try setting a parameter with a name that is not known,
        ///         the setter will throw.  You cannot add new oauth parameters
        ///         using the setter on this indexer.
        ///     </para>
        /// </remarks>
        public string this[string ix]
        {
            get
            {
                if (_params.ContainsKey(ix))
                {
                    return _params[ix];
                }
                throw new ArgumentException(ix);
            }
            set
            {
                if (!_params.ContainsKey(ix))
                {
                    throw new ArgumentException(ix);
                }
                _params[ix] = value;
            }
        }

        /// <summary>
        ///     Generate the timestamp for the signature.
        /// </summary>
        /// <returns>The timestamp, in string form.</returns>
        private string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - _epoch;
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        ///     Renews the nonce and timestamp on the oauth parameters.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each new request should get a new, current timestamp, and a
        ///         nonce. This helper method does both of those things. This gets
        ///         called before generating an authorization header, as for example
        ///         when the user of this class calls <see cref='AcquireRequestToken'>.
        ///     </para>
        /// </remarks>
        private void NewRequest()
        {
            _params["nonce"] = GenerateNonce();
            _params["timestamp"] = GenerateTimeStamp();
        }

        /// <summary>
        ///     Generate an oauth nonce.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         According to RFC 5849, A nonce is a random string,
        ///         uniquely generated by the client to allow the server to
        ///         verify that a request has never been made before and
        ///         helps prevent replay attacks when requests are made over
        ///         a non-secure channel.  The nonce value MUST be unique
        ///         across all requests with the same timestamp, client
        ///         credentials, and token combinations.
        ///     </para>
        ///     <para>
        ///         One way to implement the nonce is just to use a
        ///         monotonically-increasing integer value.  It starts at zero and
        ///         increases by 1 for each new request or signature generated.
        ///         Keep in mind the nonce needs to be unique only for a given
        ///         timestamp!  So if your app makes less than one request per
        ///         second, then using a static nonce of "0" will work.
        ///     </para>
        ///     <para>
        ///         Most oauth nonce generation routines are waaaaay over-engineered,
        ///         and this one is no exception.
        ///     </para>
        /// </remarks>
        /// <returns>the nonce</returns>
        private string GenerateNonce()
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < 8; i++)
            {
                int g = _random.Next(3);
                switch (g)
                {
                    case 0:
                        // lowercase alpha
                        sb.Append((char)(_random.Next(26) + 97), 1);
                        break;
                    default:
                        // numeric digits
                        sb.Append((char)(_random.Next(10) + 48), 1);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Internal function to extract from a URL all query string
        ///     parameters that are not related to oauth - in other words all
        ///     parameters not begining with "oauth_".
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, given a url like http://foo?a=7&guff, the
        ///         returned value will be a Dictionary of string-to-string
        ///         relations.  There will be 2 entries in the Dictionary: "a"=>7,
        ///         and "guff"=>"".
        ///     </para>
        /// </remarks>
        /// <param name="queryString">The query string part of the Url</param>
        /// <returns>
        ///     A Dictionary containing the set of
        ///     parameter names and associated values
        /// </returns>
        private Dictionary<string, string> ExtractQueryParameters(string queryString)
        {
            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Remove(0, 1);
            }

            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(queryString))
            {
                return result;
            }

            foreach (string s in queryString.Split('&'))
            {
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("oauth_"))
                {
                    if (s.IndexOf('=') > -1)
                    {
                        string[] temp = s.Split('=');
                        result.Add(temp[0], temp[1]);
                    }
                    else
                    {
                        result.Add(s, string.Empty);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     This is an oauth-compliant Url Encoder.  The default .NET
        ///     encoder outputs the percent encoding in lower case.  While this
        ///     is not a problem with the percent encoding defined in RFC 3986,
        ///     OAuth (RFC 5849) requires that the characters be upper case
        ///     throughout OAuth.
        /// </summary>
        /// <param name="value">The value to encode</param>
        /// <returns>the Url-encoded version of that string</returns>
        public static string UrlEncode(string value)
        {
            var result = new System.Text.StringBuilder();
            foreach (char symbol in value)
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    result.Append('%' + string.Format("{0:X2}", (int)symbol));
                }
            }
            return result.ToString();
        }

        /// <summary>
        ///     Formats the list of request parameters into string a according
        ///     to the requirements of oauth. The resulting string could be used
        ///     in the Authorization header of the request.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         See http://dev.twitter.com/pages/auth#intro  for some
        ///         background.  The output of this is not suitable for signing.
        ///     </para>
        ///     <para>
        ///         There are 2 formats for specifying the list of oauth
        ///         parameters in the oauth spec: one suitable for signing, and
        ///         the other suitable for use within Authorization HTTP Headers.
        ///         This method emits a string suitable for the latter.
        ///     </para>
        /// </remarks>
        /// <param name="parameters">
        ///     The Dictionary of
        ///     parameters. It need not be sorted.
        /// </param>
        /// <returns>a string representing the parameters</returns>
        private static string EncodeRequestParameters(ICollection<KeyValuePair<string, string>> p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> item in p.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(item.Value) &&
                    !item.Key.EndsWith("secret"))
                {
                    sb.AppendFormat("oauth_{0}=\"{1}\", ",
                        item.Key,
                        UrlEncode(item.Value));
                }
            }

            return sb.ToString().TrimEnd(' ').TrimEnd(',');
        }

        /// <summary>
        ///     Acquire a request token, from the given URI, using the given
        ///     HTTP method.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         To use this method, first instantiate a new Oauth.OAuthManager object,
        ///         then set the callback param (oauth["callback"]='oob'). After the
        ///         call returns, you should direct the user to open a browser window
        ///         to the authorization page for the OAuth-enabled service. Or,
        ///         you can automatically open that page yourself. Do this with
        ///         System.Diagnostics.Process.Start(), passing the URL of the page.
        ///         There should be one query param: oauth_token with the value
        ///         obtained from oauth["token"].
        ///     </para>
        ///     <para>
        ///         According to the OAuth spec, you need to do this only ONCE per
        ///         application.  In other words, the first time the application
        ///         is run.  The normal oauth workflow is:  (1) get a request token,
        ///         (2) use that to acquire an access token (which requires explicit
        ///         user approval), then (3) using that access token, invoke
        ///         protected services.  The first two steps need to be done only
        ///         once per application.
        ///     </para>
        ///     <para>
        ///         For Twitter, at least, you can cache the access tokens
        ///         indefinitely; Twitter says they never expire.  However, other
        ///         oauth services may not do the same. Also: the user may at any
        ///         time revoke his authorization for your app, in which case you
        ///         need to perform the first 2 steps again.
        ///     </para>
        /// </remarks>
        /// <seealso cref='AcquireAccessToken'>
        ///     </example>
        ///     <returns>
        ///         a response object that contains the entire text of the response,
        ///         as well as extracted parameters. This method presumes the
        ///         response is query-param encoded. In other words,
        ///         poauth_token=foo&something_else=bar.
        ///     </returns>
        public OAuthResponse AcquireRequestToken(string uri, string method)
        {
            NewRequest();
            string authHeader = GetAuthorizationHeader(uri, method);
            // prepare the token request
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            request.Headers.Add("Authorization", authHeader);
            request.Method = method;

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    var r = new OAuthResponse(reader.ReadToEnd());
                    this["token"] = r["oauth_token"];

                    // Sometimes the request_token URL gives us an access token,
                    // with no user interaction required. Eg, when prior approval
                    // has already been granted.
                    try
                    {
                        if (r["oauth_token_secret"] != null)
                        {
                            this["token_secret"] = r["oauth_token_secret"];
                        }
                    }
                    catch
                    {
                    }
                    return r;
                }
            }
        }

        /// <summary>
        ///     Acquire an access token, from the given URI, using the given
        ///     HTTP method.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         To use this method, you must first set the oauth_token to the value
        ///         of the request token.  Eg, oauth["token"] = "whatever".
        ///     </para>
        ///     <para>
        ///         According to the OAuth spec, you need to do this only ONCE per
        ///         application.  In other words, the first time the application
        ///         is run.  The normal oauth workflow is:  (1) get a request token,
        ///         (2) use that to acquire an access token (which requires explicit
        ///         user approval), then (3) using that access token, invoke
        ///         protected services.  The first two steps need to be done only
        ///         once per application.
        ///     </para>
        ///     <para>
        ///         For Twitter, at least, you can cache the access tokens
        ///         indefinitely; Twitter says they never expire.  However, other
        ///         oauth services may not do the same. Also: the user may at any
        ///         time revoke his authorization for your app, in which case you
        ///         need to perform the first 2 steps again.
        ///     </para>
        /// </remarks>
        /// <seealso cref='AcquireRequestToken'>
        ///     </example>
        ///     <returns>
        ///         a response object that contains the entire text of the response,
        ///         as well as extracted parameters. This method presumes the
        ///         response is query-param encoded. In other words,
        ///         poauth_token=foo&something_else=bar.
        ///     </returns>
        public OAuthResponse AcquireAccessToken(string uri, string method, string pin)
        {
            NewRequest();
            _params["verifier"] = pin;

            string authHeader = GetAuthorizationHeader(uri, method);

            // prepare the token request
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            request.Headers.Add("Authorization", authHeader);
            request.Method = method;

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    var r = new OAuthResponse(reader.ReadToEnd());
                    this["token"] = r["oauth_token"];
                    this["token_secret"] = r["oauth_token_secret"];
                    return r;
                }
            }
        }

        /// <summary>
        ///     Generate a string to be used in an Authorization header in
        ///     an HTTP request.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method assembles the available oauth_ parameters that
        ///         have been set in the Dictionary in this instance, produces
        ///         the signature base (As described by the OAuth spec, RFC 5849),
        ///         signs it, then re-formats the oauth_ parameters into the
        ///         appropriate form, including the oauth_signature value, and
        ///         returns the result.
        ///     </para>
        /// </remarks>
        /// <seealso cref='GenerateauthHeader'>
        public string GenerateauthHeader(string uri, string method)
        {
            NewRequest();
            string authHeader = GetAuthorizationHeader(uri, method, null);
            return authHeader;
        }

        private string GetAuthorizationHeader(string uri, string method, string realm = null)
        {
            if (string.IsNullOrEmpty(_params["consumer_key"]))
            {
                throw new ArgumentNullException("consumer_key");
            }

            if (string.IsNullOrEmpty(_params["signature_method"]))
            {
                throw new ArgumentNullException("signature_method");
            }

            Sign(uri, method);

            string erp = EncodeRequestParameters(_params);
            return (string.IsNullOrEmpty(realm))
                ? "OAuth " + erp
                : string.Format("OAuth realm=\"{0}\", ", realm) + erp;
        }

        private void Sign(string uri, string method)
        {
            string signatureBase = GetSignatureBase(uri, method);
            HashAlgorithm hash = GetHash();

            byte[] dataBuffer = System.Text.Encoding.ASCII.GetBytes(signatureBase);
            byte[] hashBytes = hash.ComputeHash(dataBuffer);

            this["signature"] = Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        ///     Formats the list of request parameters into "signature base" string as
        ///     defined by RFC 5849.  This will then be MAC'd with a suitable hash.
        /// </summary>
        private string GetSignatureBase(string url, string method)
        {
            // normalize the URI
            var uri = new Uri(url);
            string normUrl = string.Format("{0}://{1}", uri.Scheme, uri.Host);
            if (!((uri.Scheme == "http" && uri.Port == 80) ||
                  (uri.Scheme == "https" && uri.Port == 443)))
            {
                normUrl += ":" + uri.Port;
            }

            normUrl += uri.AbsolutePath;

            // the sigbase starts with the method and the encoded URI
            var sb = new System.Text.StringBuilder();
            sb.Append(method)
              .Append('&')
              .Append(UrlEncode(normUrl))
              .Append('&');

            // the parameters follow - all oauth params plus any params on
            // the uri
            // each uri may have a distinct set of query params
            Dictionary<string, string> p = ExtractQueryParameters(uri.Query);
            // add all non-empty params to the "current" params
            foreach (KeyValuePair<string, string> p1 in _params)
            {
                // Exclude all oauth params that are secret or
                // signatures; any secrets should be kept to ourselves,
                // and any existing signature will be invalid.
                if (!string.IsNullOrEmpty(_params[p1.Key]) &&
                    !p1.Key.EndsWith("_secret") &&
                    !p1.Key.EndsWith("signature"))
                {
                    p.Add("oauth_" + p1.Key, p1.Value);
                }
            }

            // concat+format all those params
            var sb1 = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> item in p.OrderBy(x => x.Key))
            {
                // even "empty" params need to be encoded this way.
                sb1.AppendFormat("{0}={1}&", item.Key, item.Value);
            }

            // append the UrlEncoded version of that string to the sigbase
            sb.Append(UrlEncode(sb1.ToString().TrimEnd('&')));
            string result = sb.ToString();
            return result;
        }

        private HashAlgorithm GetHash()
        {
            if (this["signature_method"] != "HMAC-SHA1")
            {
                throw new NotImplementedException();
            }

            string keystring = string.Format("{0}&{1}",
                UrlEncode(this["consumer_secret"]),
                UrlEncode(this["token_secret"]));
            var hmacsha1 = new HMACSHA1
            {
                Key = System.Text.Encoding.ASCII.GetBytes(keystring)
            };
            return hmacsha1;
        }
    }

    /// <summary>
    ///     A class to hold an OAuth response message.
    /// </summary>
    public class OAuthResponse
    {
        private readonly Dictionary<string, string> _params;

        public OAuthResponse(string alltext)
        {
            AllText = alltext;
            _params = new Dictionary<string, string>();
            string[] kvpairs = alltext.Split('&');
            foreach (string pair in kvpairs)
            {
                string[] kv = pair.Split('=');
                _params.Add(kv[0], kv[1]);
            }
            // expected keys:
            //   oauth_token, oauth_token_secret, user_id, screen_name, etc
        }

        /// <summary>
        ///     All of the text in the response. This is useful if the app wants
        ///     to do its own parsing.
        /// </summary>
        public string AllText { get; set; }

        /// <summary>
        ///     a Dictionary of response parameters.
        /// </summary>
        public string this[string ix]
        {
            get { return _params[ix]; }
        }
    }
}
