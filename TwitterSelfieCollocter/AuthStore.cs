using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitterSelfieCollocter;

namespace SimpleOneDrive
{
    public class AuthStore
    {
        public string client_id { get; set; }
        public string scope { get; set; }
        public string redirect_uri { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string client_secret { get; set; }
        public double expired_datetime { get; set; }
        public bool IsExpired { get { return expired_datetime - 5 * 60 < TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds; } }

        public const string Define = "AuthStore.json";
        private static object syncRoot = new Object();

        public static AuthStore Instance
        {
            get
            {
                if (!System.IO.File.Exists(Define))
                {
                    System.IO.File.WriteAllText(Define,
                     JsonConvert.SerializeObject(new AuthStore(),
                     Formatting.Indented));
                }

                if (_Instance == null)
                    lock (syncRoot)
                        if (_Instance == null)
                        {
                            _Instance = JsonConvert.DeserializeObject<AuthStore>(
                           System.IO.File.ReadAllText(Define));
                        }

                return _Instance;
            }
        }

        private static AuthStore _Instance = null;

        public void save()
        {
            System.IO.File.WriteAllText(Define,
                    JsonConvert.SerializeObject(this,
                    Formatting.Indented));
        }

        public bool TryRefreshToken()
        {
            try
            {
                if (!IsExpired)
                {
                    return true;
                }

                DebugLogger.Instance.W("reauth!");

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://login.microsoftonline.com");
                    var content = new FormUrlEncodedContent(new[]
                    {
                             new KeyValuePair<string, string>("client_id", this.client_id),
                             new KeyValuePair<string, string>("redirect_uri", this.redirect_uri),
                             new KeyValuePair<string, string>("client_secret", this.client_secret),
                             new KeyValuePair<string, string>("refresh_token", this.refresh_token),
                             new KeyValuePair<string, string>("grant_type", "refresh_token")
                        });
                    var result = client.PostAsync("/common/oauth2/v2.0/token", content).Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    DebugLogger.Instance.W(resultContent);
                    var authresult = JObject.Parse(resultContent);
                    this.refresh_token = authresult["refresh_token"].ToString();
                    this.access_token = authresult["access_token"].ToString();
                    this.expired_datetime = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds +
                                            double.Parse(authresult["expires_in"].ToString());
                    this.save();
                }
                Thread.Sleep(500);
                return true;
            }
            catch (Exception e)
            {
                DebugLogger.Instance.W(e.Message);
                return false;
            }
        }

        public string GetToken()
        {
            if (IsExpired) TryRefreshToken();
            return access_token;
        }

        public static bool TryAuthentication(string client_id, string client_secret, string redirect_uri, string code)
        {
            try
            {
                AuthStore aus = new AuthStore();
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://login.microsoftonline.com");
                    var content = new FormUrlEncodedContent(new[]
                    {
                             new KeyValuePair<string, string>("client_id", client_id),
                             new KeyValuePair<string, string>("redirect_uri", redirect_uri),
                             new KeyValuePair<string, string>("client_secret", client_secret),
                             new KeyValuePair<string, string>("code", code),
                             new KeyValuePair<string, string>("grant_type", "authorization_code")
                        });
                    var result = client.PostAsync("/common/oauth2/v2.0/token", content).Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    var authresult = JObject.Parse(resultContent);
                    aus.refresh_token = authresult["refresh_token"].ToString();
                    aus.access_token = authresult["access_token"].ToString();
                    aus.client_id = client_id;
                    aus.client_secret = client_secret;
                    aus.redirect_uri = redirect_uri;
                    aus.scope = "files.readwrite+offline_access";
                    aus.expired_datetime = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds +
                                            double.Parse(authresult["expires_in"].ToString());
                    aus.save();
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

    }
}
