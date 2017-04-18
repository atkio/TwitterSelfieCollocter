using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TwitterSelfieCollocter;

namespace SimpleOneDrive
{
    public class SimpleClient
    {

        #region Define
        private static object syncRoot = new Object();

        private SimpleClient()
        {

        }

        public static SimpleClient Instance
        {
            get
            {

                if (_Instance == null)
                    lock (syncRoot)
                    {
                        if (_Instance == null)
                            _Instance = new SimpleClient();
                    }


                return _Instance;
            }
        }

        const string apistring = "https://graph.microsoft.com/v1.0/me";

        private static SimpleClient _Instance = null;

        public string AccessToken
        {
            get { return AuthStore.Instance.GetToken(); }
        }

        #endregion


        /// <summary>
        /// フォルダの新規作成
        /// https://dev.onedrive.com/items/create.htm
        /// </summary>
        /// <param name="parentID"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task<OnedriveItemMeta> CreateFolder(string folder, string parentID = null)
        {

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Post,
                    (parentID == null) ?
                    new Uri(apistring + "/drive/root/children") :
                    new Uri(apistring + string.Format("/drive/items/{0}/children", parentID))
                );


                request.Content = new StringContent("{\"name\": \"" + folder + "\",\"folder\": { }}", Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                DebugLogger.Instance.W(responseText);

                OnedriveItemMeta item = JsonConvert.DeserializeObject<OnedriveItemMeta>(responseText);

                return item;
            }

        }

        /// <summary>
        /// ファイル削除処理
        /// https://dev.onedrive.com/items/delete.htm
        /// </summary>
        /// <param name="itemid"></param>
        /// <returns></returns>
        public async Task deleteItem(string itemid)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    new Uri(apistring + string.Format("/drive/items/{0}", itemid))
                );
                var response = await httpClient.SendAsync(request);
                DebugLogger.Instance.W(response.Content.ReadAsStringAsync().Result);
            }
        }

        /// <summary>
        /// ダウンロード処理
        /// https://dev.onedrive.com/items/download.htm
        /// </summary>
        /// <param name="itemid"></param>
        /// <param name="localfilename"></param>
        /// <returns></returns>
        public async Task downloadFile(string itemid, string localfilename)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    new Uri(apistring + string.Format("/drive/items/{0}/content", itemid))
                );
                var response = await httpClient.SendAsync(request);
                await ReadAsFileAsync(response.Content, localfilename);
            }
        }

        /// <summary>
        /// ローカル保存
        /// </summary>
        /// <param name="content"></param>
        /// <param name="pathname"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        private static Task ReadAsFileAsync(HttpContent content, string pathname)
        {



            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                return content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        fileStream.Close();
                    });
            }
            catch
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }

                throw;
            }
        }

        /// <summary>
        /// アイテムを取得
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public async Task<OnedriveItemMeta> getItem(string itemID = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    itemID == null ?
                    new Uri(apistring + "/drive/root?expand=children") :
                    new Uri(apistring + string.Format("/drive/items/{0}?expand=children", itemID))
                );
                var response = await httpClient.SendAsync(request);
                OnedriveItemMeta item = JsonConvert.DeserializeObject<OnedriveItemMeta>(response.Content.ReadAsStringAsync().Result);

                return item;
            }
        }

        /// <summary>
        /// ファイル一覧表示
        /// https://dev.onedrive.com/items/list.htm
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        public async Task<List<OnedriveItemMeta>> listChildrenItems(string parentID = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    parentID == null ?
                    new Uri(apistring + "/drive/root/children") :
                    new Uri(apistring + string.Format("/drive/items/{0}/children", parentID))
                );
                var response = await httpClient.SendAsync(request);
                OnedriveListMeta files = JsonConvert.DeserializeObject<OnedriveListMeta>(response.Content.ReadAsStringAsync().Result);

                return files.value;
            }

        }

        /// <summary>
        /// アイテムを検索
        /// https://dev.onedrive.com/items/search.htm
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentID"></param>
        /// <returns></returns>
        public async Task<OnedriveSearchMeta> searchMetafromName(string name, string parentID = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    parentID == null ?
                    new Uri(apistring + string.Format("/drive/root/search(q='{0}')", name)) :
                    new Uri(apistring + string.Format("/drive/items/{0}/search(q='{1}')", parentID, name))
                );
                var response = await httpClient.SendAsync(request);

                DebugLogger.Instance.W(response.Content.ReadAsStringAsync().Result);
                OnedriveSearchMeta rs = JsonConvert.DeserializeObject<OnedriveSearchMeta>(response.Content.ReadAsStringAsync().Result);

                return rs;
            }

        }

        /// <summary>
        /// ROOTIDを取得
        /// </summary>
        /// <returns></returns>
        public async Task<string> getRootID()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    new Uri(apistring + "/drive/root")
                );
                var response = await httpClient.SendAsync(request);

                DebugLogger.Instance.W(response.Content.ReadAsStringAsync().Result);

                OnedriveItemMeta files = JsonConvert.DeserializeObject<OnedriveItemMeta>(response.Content.ReadAsStringAsync().Result);

                return files.id;
            }
        }


        // 
        /// <summary>
        /// ファイル アップロード処理
        /// https://dev.onedrive.com/items/upload_put.htm
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="remotepathid"></param>
        /// <returns></returns>
        public async Task<OnedriveItemMeta> uploadFile(string localFile, string remotepathid = null)
        {

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "octet-stream");
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Put,
                    remotepathid == null ?
                    new Uri(apistring + string.Format("/drive/root:/{0}:/content", new FileInfo(localFile).Name)) :
                    new Uri(apistring + string.Format("/drive/items/{0}:/{1}:/content", remotepathid, new FileInfo(localFile).Name))
                );
                request.Content = new ByteArrayContent(File.ReadAllBytes(localFile));
                var response = await httpClient.SendAsync(request);

                var responseText = await response.Content.ReadAsStringAsync();

                DebugLogger.Instance.W(responseText);

                return JsonConvert.DeserializeObject<OnedriveItemMeta>(responseText);
            }
        }


        /// <summary>
        /// ファイル アップロード処理(URLから)
        /// https://dev.onedrive.com/items/upload_url.htm
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="remotepathid"></param>
        /// <returns></returns>
        public async Task uploadFileFromUrl(string url, string name, string remotepathid = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Prefer", "respond-async");
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Post,
                     remotepathid == null ?
                    new Uri(apistring + "/drive/root/children") :
                    new Uri(apistring + string.Format("/drive/items/{0}/children", remotepathid))
                );
                var content = new Dictionary<string, object>();
                content.Add("@microsoft.graph.sourceUrl", url);
                content.Add("name", name);
                content.Add("file", new List<string>());
                request.Content = new StringContent("{\"@microsoft.graph.sourceUrl\": \"" + url + "\",\"name\": \"" + name + "\",\"file\": { }}",
                                                       Encoding.UTF8, "application/json");
                var response = await httpClient.SendAsync(request);

                DebugLogger.Instance.W(response.Content.ReadAsStringAsync().Result);
            }
        }



        // ファイル名の変更処理
        public async Task renameFile(string remotePath, string name, string newname)
        {

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                HttpRequestMessage request = new HttpRequestMessage(
                    new HttpMethod("PATCH"),
                    new Uri(string.Format("https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/{1}", remotePath, name))
                );

                MyFileModify filemod = new MyFileModify();
                filemod.name = newname;
                request.Content = new StringContent(JsonConvert.SerializeObject(filemod), Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);

            }

        }

    }

    #region OnedriveMeta

    public class OnedriveSearchMeta
    {
        public List<OnedriveItemMeta> value;
    }

    public class OnedriveItemMeta
    {
        public string id { get; set; }
        public string name { get; set; }
        public string webUrl { get; set; }
        public string createdDateTime { get; set; }
        public string lastModifiedDateTime { get; set; }
        public OneFolderMeta folder { get; set; }
        public List<OnedriveItemMeta> children { get; set; }

    }

    public class OneFolderMeta
    {
        public Int64 childCount { get; set; }
    }

    public class OnedriveListMeta
    {
        public List<OnedriveItemMeta> value;
    }

    // ファイル移動時に使います。
    public class MyParentFolder
    {
        public string path { get; set; }
    }

    public class MyFileModify
    {
        public string name { get; set; }
        // ファイル移動時に使います。
        public MyParentFolder parentReference { get; set; }
    }

    #endregion
}
