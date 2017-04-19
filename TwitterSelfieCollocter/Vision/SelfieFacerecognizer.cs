using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SimpleOneDrive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterSelfieCollocter
{
    class SelfieFacerecognizer
    {
        private static volatile SelfieFacerecognizer instance;
        private static object syncRoot = new Object();

        public static SelfieFacerecognizer Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new SelfieFacerecognizer();
                    }
                }

                return instance;
            }
        }

        private SelfieFacerecognizer()
        {


        }


        /// <summary>
        /// 查所有需要识别的图片
        /// </summary>
        public void checkALL()
        {
            var db = new SelfieBotDB();
            var nrs = db.getAllWaitRecognizer();
            SelfieBotConfig config = SelfieBotConfig.Instance;
           
            List<WaitRecognizer> isfaces;
       

            //本地查出有脸图片
            isfaces = nrs.Where(n => Detect(n.PhotoPath))
                         .ToList();

            DebugLogger.Instance.W("found faces >" + isfaces.Count);


            if (config.onedrive.IsValue)
            {
                uploadtoonedrive(isfaces);
            }
           
            copytolocal(config, isfaces);
            

            db.removeAllWaitRecognizer();

            DebugLogger.Instance.W("removeAllWaitRecognizer");

            foreach (FileInfo file in new DirectoryInfo(config.PhotoTempPath).GetFiles())
            {
                file.Delete();
            }

            DebugLogger.Instance.W("Deleted all files");
        }

        private static void copytolocal(SelfieBotConfig config, List<WaitRecognizer> isfaces)
        {
            foreach (var tid in isfaces)
            {
                var targetPath = Path.Combine(config.PhotoPath, tid.UID);
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
                try
                {
                    File.Copy(tid.PhotoPath, Path.Combine(targetPath, new FileInfo(tid.PhotoPath).Name));
                }
                catch (Exception e)
                {
                    DebugLogger.Instance.W(e.Message);

                }
            }
        }

        private void uploadtoonedrive(List<WaitRecognizer> isfaces)
        {
            isfaces.GroupBy(f => f.UID)
                .ToList()
                .ForEach(us =>
                {
                    try
                    {
                        var pid = checkuserpath(us.Key).Result;
                        foreach (var f in us)
                        {
                            DebugLogger.Instance.W("upfile:"+f.PhotoUrl +"|name:"+ new FileInfo(f.PhotoPath).Name+"|pid:"+pid);
                            Task.Run(async () => { await 
                                SimpleClient.Instance.uploadFileFromUrl(f.PhotoUrl, new FileInfo(f.PhotoPath).Name, pid);}).Wait();
                            Thread.Sleep(500);
                        }
                    }
                    catch(Exception e)
                    {
                        DebugLogger.Instance.W(e.Message);
                        DebugLogger.Instance.W(e.StackTrace);
                    }

                });
        }

        private async Task<string> checkuserpath(string pathname)
        {
           
                    var item = await SimpleClient.Instance.CreateFolder(pathname,SelfieBotConfig.Instance.onedrive.RemoteRootID);
                    if (item.id == null)
                        throw new Exception("can not create folder:" + pathname);

                    return item.id;

          


        }

        protected static bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        #region 本地检测
        const string anifaceFileName = "lbpcascade_animeface.xml";
        const string faceFileName2 = "haarcascade_frontalface_default.xml";
        const string eyeFileName = "haarcascade_eye.xml";
        const string faceFileName = "visionary_FACES_01_LBP_5k_7k_50x50.xml";


        public static bool Detect(string file)
        {
            try
            {
                using (CascadeClassifier aniface = new CascadeClassifier(anifaceFileName))
                using (CascadeClassifier face = new CascadeClassifier(faceFileName))
                using (CascadeClassifier face2 = new CascadeClassifier(faceFileName2))
                using (UMat ugray = new UMat())
                using (Image<Bgr, byte> image = new Image<Bgr, byte>(file))
                {
                    CvInvoke.CvtColor(image.Mat, ugray, ColorConversion.Bgr2Gray);
                    CvInvoke.EqualizeHist(ugray, ugray);

                    /*     卡通人物false      */
                    if (aniface.DetectMultiScale(
                        ugray,
                        1.1,
                        10,
                        new Size(20, 20)).Count() > 0)
                        return false;

                    /*    haarcascade_frontalface   判断true */
                    if (face.DetectMultiScale(
                        ugray,
                        1.1,
                        10,
                        new Size(20, 20)).Count() > 0)
                        return true;

                    /*   Visionary_FACES   判断true */
                    if (face2.DetectMultiScale(
                        ugray,
                        1.1,
                        10,
                        new Size(20, 20)).Count() > 0)
                        return true;

                    // 判断后无人脸
                    return false;

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                DebugLogger.Instance.W(e.StackTrace);
                return false;
            }
            finally
            {
                //防止OPENCV内存泄露
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }



        }
        #endregion
        
    }


}
