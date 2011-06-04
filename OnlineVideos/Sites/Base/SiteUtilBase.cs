using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// The abstract base class for all utilities.
    /// </summary>
    public abstract class SiteUtilBase : ICustomTypeDescriptor
    {
        /// <summary>
        /// The <see cref="SiteSettings"/> as configured in the xml will be set after an instance of this class was created 
        /// by the default implementation of the <see cref="Initialize"/> method.
        /// </summary>
        public virtual SiteSettings Settings { get; protected set; }

        /// <summary>
        /// You should always call this implementation, even when overriding it. It is called after the instance has been created
        /// in order to configure settings from the xml for this util.
        /// </summary>
        /// <param name="siteSettings"></param>
        public virtual void Initialize(SiteSettings siteSettings)
        {
            Settings = siteSettings;

            // apply custom settings
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs.Length > 0)
                {
                    if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosConfiguration"
                        && siteSettings != null
                        && siteSettings.Configuration != null
                        && siteSettings.Configuration.ContainsKey(field.Name))
                    {
                        try
                        {
                            if (field.FieldType.IsEnum)
                            {
                                field.SetValue(this, Enum.Parse(field.FieldType, siteSettings.Configuration[field.Name]));
                            }
                            else
                            {
                                field.SetValue(this, Convert.ChangeType(siteSettings.Configuration[field.Name], field.FieldType));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("{0} - could not set Configuration Value: {1}. Error: {2}", siteSettings.Name, field.Name, ex.Message);
                        }
                    }
                    else if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration"
                             && OnlineVideoSettings.Instance.UserStore != null)
                    {
                        string value = OnlineVideoSettings.Instance.UserStore.GetValue(string.Format("{0}.{1}", Utils.GetSaveFilename(siteSettings.Name).Replace(' ', '_'), field.Name));
                        if (value != null)
                        {
                            try
                            {
                                if (field.FieldType.IsEnum)
                                {
                                    field.SetValue(this, Enum.Parse(field.FieldType, value));
                                }
                                else
                                {
                                    field.SetValue(this, Convert.ChangeType(value, field.FieldType));
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warn("{0} - ould not set User Configuration Value: {1}. Error: {2}", siteSettings.Name, field.Name, ex.Message);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is the only function a subclass has to implement. It's called when a user selects a category in the GUI. 
        /// It should return a list of videos for that category, reset the paging indexes, remember this category, whatever is needed to hold state.
        /// </summary>
        /// <param name="category">The <see cref="Category"/> that was selected by the user.</param>
        /// <returns>a list of <see cref="VideoInfo"/> object for display</returns>
        public abstract List<VideoInfo> getVideoList(Category category);

        /// <summary>
        /// If the site's categories can be retrieved dynamically, then it should be done in the implementation of this method.
        /// The categories must be added to the <see cref="SiteSettings"/> retrieved from the <see cref="Settings"/> property of this class.
        /// Once the categories are added you should set <see cref="SiteSettings.DynamicCategoriesDiscovered"/> to true, 
        /// so this method won't be called each time the user enters this site in the GUI (unless youi want that behavior).<br/>
        /// default: sets <see cref="SiteSettings.DynamicCategoriesDiscovered"/> to true
        /// </summary>
        /// <returns>The number of dynamic categories added. 0 means none found / added</returns>
        public virtual int DiscoverDynamicCategories()
        {
            Settings.DynamicCategoriesDiscovered = true;
            return 0;
        }

        /// <summary>
        /// If a category has sub-categories this function will be called when the user selects a category in the GUI.
        /// This happens only when <see cref="Category.HasSubCategories"/> is true and <see cref="Category.SubCategoriesDiscovered"/> is false.
        /// </summary>
        /// <param name="parentCategory">the category that was selected by the user</param>
        /// <returns>0 if no sub-categeories where found, otherwise the number of categories that were added to <see cref="Category.SubCategories"/></returns>
        public virtual int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategoriesDiscovered = true;
            return 0;
        }

        /// <summary>
        /// This will be called to find out if there is a next page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "next page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public virtual bool HasNextPage { get; protected set; }

        /// <summary>
        /// This function should return the videos of the next page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasNextPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "next page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the next page of the last queried category.</returns>
        public virtual List<VideoInfo> getNextPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This will be called to find out if there is a previous page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "previous page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public virtual bool HasPreviousPage { get; protected set; }

        /// <summary>
        /// This function should return the videos of the previous page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasPreviousPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "previous page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the previous page of the last queried category.</returns>
        public virtual List<VideoInfo> getPreviousPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This function will be called to get the urls for playback of a video.<br/>
        /// By default: returns a list with the result from <see cref="getUrl"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
        /// <returns></returns>
        public virtual List<String> getMultipleVideoUrls(VideoInfo video)
        {
            List<String> urls = new List<String>();
            urls.Add(getUrl(video));
            return urls;
        }

        /// <summary>
        /// Allows the Util to resolve the url of a playlist item to playback options or a new url directly before playback.
        /// </summary>
        /// <param name="clonedVideoInfo">A clone of the original <see cref="VideoInfo"/> object, given in <see cref="getMultipleVideoUrls"/>, with the VideoUrl set to one of the urls returned.</param>
        /// <param name="chosenPlaybackOption">the key from the <see cref="VideoInfo.PlaybackOptions"/> of the first video chosen by the user</param>
        /// <returns>the resolved url (by default just the clonedVideoInfo.VideoUrl that was given in <see cref="getMultipleVideoUrls"/></returns>
        public virtual string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption)
        {
            return clonedVideoInfo.VideoUrl;
        }

        /// <summary>
        /// This function will be called when the user selects a video for playback. It should return the absolute url to the video file.<br/>
        /// By default, the <see cref="VideoInfo.VideoUrl"/> fields value will be returned.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> from the list of displayed videos that were returned by this instance previously.</param>
        /// <returns>A valid url or filename.</returns>
        public virtual String getUrl(VideoInfo video)
        {
            return video.VideoUrl;
        }

        #region Search
        /// <summary>
        /// Returns true, if this site allows searching.<br/>
        /// default: false
        /// </summary>
        public virtual bool CanSearch
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true, if this site has categories to filter on, f.e. newest, highest rating.<br/>
        /// default: false
        /// </summary>
        public virtual bool HasFilterCategories
        {
            get { return false; }
        }

        /// <summary>
        /// Will be called to get the list of categories (names only) that can be chosen to search. 
        /// The keys will be the names and the value will be given to the <see cref="Search"/> as parameter.<br/>
        /// default: returns empty list, so no category specific search can be done
        /// This is also used if HasFilterCategories returns true
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> for the given query.
        /// </summary>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos matching that search query.</returns>
        public virtual List<VideoInfo> Search(string query)
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> for the given query limited to the given category.<br/>
        /// default: calls the Search overload without a category parameter
        /// </summary>        
        /// <param name="category">The category to search in.</param>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos matching that search query.</returns>
        public virtual List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        /// <summary>
        /// Should return the title of the current page, which will be put in #header.label at state=videos
        /// </summary>
        /// <returns>the title of the current page</returns>
        public virtual string getCurrentVideosTitle()
        {
            return null;
        }

        #endregion

        /// <summary>
        /// This method will be called before downloading the video from the given url, 
        /// or before adding it to the favorites. (in that case the url param is null.<br/>
        /// By default, the favorite name is the <see cref="VideoInfo.Title"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object that can be used to get some more info.</param>
        /// <param name="category">The <see cref="Category"/> that this video comes from.</param>
        /// <param name="url">The url from which the download will take place. If null, a favorite name should be returned.</param>
        /// <returns>A cleaned pretty name that can be user as filename, or as favorite title.</returns>
        public virtual string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            if (string.IsNullOrEmpty(url)) // called for adding to favorites
                return video.Title;
            else // called for downloading
            {
                string extension = System.IO.Path.GetExtension(new System.Uri(url).LocalPath.Trim(new char[] { '/' }));
                if (extension == string.Empty) extension = System.IO.Path.GetExtension(url);
                if (extension == ".f4v" || extension == ".fid") extension = ".flv";
                string safeName = Utils.GetSaveFilename(video.Title);
                return safeName + extension;
            }
        }

        /// <summary>
        /// This function checks a given string, if it points to a file that is a valid video.
        /// It will check for protocol and known and supported video extensions.
        /// </summary>
        /// <param name="fsUrl">the string to check which should be a valid URI.</param>
        /// <returns>true if the url points to a video that can be played with directshow.</returns>
        public virtual bool isPossibleVideo(string fsUrl)
        {
            if (string.IsNullOrEmpty(fsUrl)) return false; // empty string is not a video
            if (fsUrl.StartsWith("rtsp://")) return false; // rtsp protocol not supported yet
            string extensionFile = System.IO.Path.GetExtension(fsUrl).ToLower();
            bool isVideo = OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extensionFile);
            if (!isVideo)
            {
                foreach (string anExt in OnlineVideoSettings.Instance.VideoExtensions.Keys) if (fsUrl.Contains(anExt)) { isVideo = true; break; }
            }
            return isVideo;
        }

        /// <summary>
        /// This function will be called when a contextmenu for a video is shown in the GUI. 
        /// Override it to add your own entries (which should be localized).
        /// </summary>
        /// <param name="selectedCategory"></param>
        /// <param name="selectedItem">when this is null the context menu was called on the category</param>
        /// <returns>A list of string to be added to the context menu for the given VideoInfo.</returns>
        public virtual List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            return new List<string>();
        }

        /// <summary>
        /// This function is called when one of the custom contextmenu entries was selected by the user.
        /// Override it to handle the entries you added with <see cref="GetContextMenuEntries"/>.
        /// </summary>
        /// <param name="selectedCategory"></param>
        /// <param name="selectedItem"></param>
        /// <param name="choice"></param>
        /// <returns>true, if videos for the current category need to be retrieved again</returns>
        public virtual bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice)
        {
            return false;
        }

        # region static helper functions

        public static string GetRedirectedUrl(string url, string referer = null)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                if (!string.IsNullOrEmpty(referer)) request.Referer = referer;
                request.Timeout = 15000;
                httpWebresponse = request.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;

                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                {
                    if (httpWebresponse.ContentLength > 0 && httpWebresponse.ContentLength < 1024)
                    {
                        string content = new StreamReader(httpWebresponse.GetResponseStream()).ReadToEnd();
                        if (httpWebresponse.ContentType.Contains("video/quicktime"))
                        {
                            return content.Split('\n')[1];
                        }
                        return httpWebresponse.ResponseUri.ToString();
                    }
                    else
                        return url;
                }
                else
                    return httpWebresponse.ResponseUri.OriginalString;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            finally
            {
                if (httpWebresponse != null) httpWebresponse.Close();
            }
            return url;
        }

        /// <summary>
        /// This method should be used whenever requesting data via http get. You can optionally provide some request settings.
        /// It will automatically convert the retrieved data into the type you provided.
        /// Retrieved data is added to a cache if HTTP Status was 200 and more than 500 bytes were retrieved. The cache timeout is user configurable (<see cref="OnlineVideoSettings.CacheTimeout"/>).
        /// </summary>
        /// <typeparam name="T">The type you want the returned data to be. Supported are <see cref="String"/>, <see cref="Newtonsoft.Json.Linq.JObject"/>, <see cref="RssToolkit.Rss.RssDocument"/>, <see cref="XmlDocument"/> and <see cref="HtmlAgilityPack.HtmlDocument"/>.</typeparam>
        /// <param name="url">The url to requets data from.</param>
        /// <param name="cc">A <see cref="CookieContainer"/> that will send cookies along with the request and afterwards contains all cookies of the response.</param>
        /// <param name="referer">A referer that will be send with the request.</param>
        /// <param name="proxy">If you want to use a proxy for the request, give a <see cref="IWebProxy"/>.</param>
        /// <param name="forceUTF8">Some server are not returning a valid CharacterSet on the response, set to true to force reading the response content as UTF8.</param>
        /// <param name="allowUnsafeHeader">Some server return headers that are treated as unsafe by .net. In order to retrieve that data set this to true.</param>
        /// <param name="userAgent">You can provide a custom UserAgent for the request, otherwise the default one (<see cref="OnlineVideoSettings.UserAgent"/>) is used.</param>
        /// <returns>The data returned by a <see cref="HttpWebResponse"/> converted to the specified type.</returns>
        public static T GetWebData<T>(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null)
        {
            string webData = GetWebData(url, cc, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)webData;
            }
            else if (typeof(T) == typeof(Newtonsoft.Json.Linq.JObject))
            {
                // attempt to convert the returned string into a Json object
                return (T)(object)Newtonsoft.Json.Linq.JObject.Parse(webData);
            }
            else if (typeof(T) == typeof(RssToolkit.Rss.RssDocument))
            {
                // attempt to convert the returned string into a Rss Document
                return (T)(object)RssToolkit.Rss.RssDocument.Load(webData);
            }
            else if (typeof(T) == typeof(XmlDocument))
            {
                // attempt to convert the returned string into a Xml Document
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(webData);
                return (T)(object)xmlDoc;
            }
            else if (typeof(T) == typeof(HtmlAgilityPack.HtmlDocument))
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(webData);
                return (T)(object)htmlDoc;
            }

            return default(T);
        }

        public static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null)
        {
            try
            {
                Log.Debug("get webdata from {0}", url);
                // try cache first
                string cachedData = WebCache.Instance[url];
                if (cachedData != null) return cachedData;

                // request the data
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent; // set specific UserAgent if given
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent; // set OnlineVideos default UserAgent
                request.Accept = "*/*"; // we accept any content type
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set referer if given
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();
                Encoding encoding = Encoding.UTF8;
                if (!forceUTF8 && !String.IsNullOrEmpty(response.CharacterSet)) encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                using (StreamReader reader = new StreamReader(responseStream, encoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[url] = str;
                    return str;
                }
            }
            finally
            {
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        public static string GetWebDataFromPost(string url, string postData, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null)
        {
            try
            {
                Log.Debug("get webdata from {0}", url);

                // request the data
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                byte[] data = Encoding.UTF8.GetBytes(postData);

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent;
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                request.Timeout = 15000;
                request.ContentLength = data.Length;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer;
                if (cc != null) request.CookieContainer = cc;
                if (proxy != null) request.Proxy = proxy;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    Encoding encoding = Encoding.UTF8;
                    if (!forceUTF8 && !String.IsNullOrEmpty(response.CharacterSet)) encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));

                    StreamReader reader = new StreamReader(responseStream, encoding, true);
                    string str = reader.ReadToEnd();
                    return str.Trim();
                }

            }
            finally
            {
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        protected static List<String> ParseASX(string url)
        {
            string lsAsxData = GetWebData(url).ToLower();
            MatchCollection videoUrls = Regex.Matches(lsAsxData, @"<ref\s+href\s*=\s*\""(?<url>[^\""]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
        }

        protected static string ParseASX(string url, out string startTime)
        {
            startTime = "";
            string lsAsxData = GetWebData(url).ToLower();
            XmlDocument asxDoc = new XmlDocument();
            asxDoc.LoadXml(lsAsxData);
            XmlElement entryElement = asxDoc.SelectSingleNode("//entry") as XmlElement;
            if (entryElement == null) return "";
            XmlElement refElement = entryElement.SelectSingleNode("ref") as XmlElement;
            if (entryElement == null) return "";
            XmlElement startElement = entryElement.SelectSingleNode("starttime") as XmlElement;
            if (startElement != null) startTime = startElement.GetAttribute("value");
            return refElement.GetAttribute("href");
        }

        #endregion

        #region ICustomTypeDescriptor Members

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        #endregion

        #region ICustomTypeDescriptor Implementation with Fields as Properties

        private PropertyDescriptorCollection _propCache;
        private FilterCache _filterCache;

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(
            Attribute[] attributes)
        {
            bool filtering = (attributes != null && attributes.Length > 0);
            PropertyDescriptorCollection props = _propCache;
            FilterCache cache = _filterCache;

            // Use a cached version if possible
            if (filtering && cache != null && cache.IsValid(attributes))
                return cache.FilteredProperties;
            else if (!filtering && props != null)
                return props;

            // Create the property collection and filter
            props = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor prop in
                TypeDescriptor.GetProperties(
                this, attributes, true))
            {
                props.Add(prop);
            }
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                FieldPropertyDescriptor fieldDesc =
                    new FieldPropertyDescriptor(field);
                if (!filtering ||
                    fieldDesc.Attributes.Contains(attributes))
                    props.Add(fieldDesc);
            }

            // Store the computed properties
            if (filtering)
            {
                cache = new FilterCache();
                cache.Attributes = attributes;
                cache.FilteredProperties = props;
                _filterCache = cache;
            }
            else _propCache = props;

            return props;
        }

        private class FieldPropertyDescriptor : PropertyDescriptor
        {
            private FieldInfo _field;

            public FieldPropertyDescriptor(FieldInfo field)
                : base(field.Name,
                    (Attribute[])field.GetCustomAttributes(typeof(Attribute), true))
            {
                _field = field;
            }

            public FieldInfo Field { get { return _field; } }

            public override bool Equals(object obj)
            {
                FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
                return other != null && other._field.Equals(_field);
            }

            public override int GetHashCode() { return _field.GetHashCode(); }

            public override bool IsReadOnly { get { return false; } }

            public override void ResetValue(object component) { }

            public override bool CanResetValue(object component) { return false; }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }

            public override Type ComponentType
            {
                get { return _field.DeclaringType; }
            }

            public override Type PropertyType { get { return _field.FieldType; } }

            public override object GetValue(object component)
            {
                return _field.GetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                // only set if changed
                if (_field.GetValue(component) != value)
                {
                    _field.SetValue(component, value);
                    OnValueChanged(component, EventArgs.Empty);

                    // if this field is a user config, set value also in MediaPortal config file
                    object[] attrs = _field.GetCustomAttributes(typeof(CategoryAttribute), false);
                    if (attrs.Length > 0 && ((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration")
                    {
                        string siteName = (component as Sites.SiteUtilBase).Settings.Name;
                        OnlineVideoSettings.Instance.UserStore.SetValue(string.Format("{0}.{1}", Utils.GetSaveFilename(siteName).Replace(' ', '_'), _field.Name), value.ToString());
                    }
                }
            }
        }

        private class FilterCache
        {
            public Attribute[] Attributes;
            public PropertyDescriptorCollection FilteredProperties;
            public bool IsValid(Attribute[] other)
            {
                if (other == null || Attributes == null) return false;

                if (Attributes.Length != other.Length) return false;

                for (int i = 0; i < other.Length; i++)
                {
                    if (!Attributes[i].Match(other[i])) return false;
                }

                return true;
            }
        }

        #endregion
    }

}
