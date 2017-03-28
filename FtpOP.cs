/*********************************************
 * 功能描述:FTP通用操作工具基于edtftpnet
 * 创 建 人:胡庆杰
 * 日    期:2016-10-1
 * 
 ********************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnterpriseDT.Net.Ftp;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;

namespace FTPCtrl
{
    public class FtpOP
    {
        #region 属性及字段
        public FTPConnection conn = null;
        private FtpOP() { }

        //是否保持连接
        public bool KeepAlive { set; get; }

        //主机
        public string Host { set; get; }

        //端口
        public string Port { set; get; }

        //用户名
        public string UserID { set; get; }

        //密码
        public string PWD { set; get; }

        //设置远程地址
        public string DirPath { set; get; }

        //当前指向的服务器端的文件路径
        public string FileName { set; get; }

        //设置是否允许SSL
        //public bool EnableSSL { set; get; }

        //设置是否使用被动模式
        //public bool UsePassive { set; get; }

        //设置是否使用二进制方式传输数据
        //public bool UseBinary { set; get; }

        #endregion

        #region 创建FtpOP对象 public static FtpOP CreateFtpOP(string host,string port,string userID,string pwd,string dirPath)
        /// <summary>
        /// 创建FtpOP对象
        /// </summary>
        /// <param name="host">主机地址 如:localhost/127.0.0.1</param>
        /// <param name="port">端口号,一般为21</param>
        /// <param name="userID">用户名</param>
        /// <param name="pwd">用户密码</param>
        /// <param name="dirPath">指向的路径,以"/"开头</param>
        /// <returns></returns>
        public static FtpOP CreateFtpOP(string host, string port, string userID, string pwd, string dirPath)
        {
            FtpOP op = new FtpOP();
            //默认设置
            op.Host = host;
            op.Port = port;
            op.UserID = userID;
            op.PWD = pwd;
            op.DirPath = dirPath;
            return op;
        }
        #endregion

        #region 解析ftp的url地址 public static Hashtable ParseUrl(string url, bool isFile)
        /// <summary>
        /// 解析ftp的url地址(指定当前地址是指向文件夹的还是具体文件的),解析出来服务器地址、端口、用户名、密码、当前所在路径、当前指向的文件的名字
        /// </summary>
        /// <param name="url">要进行解析的url地址</param>
        /// <param name="isFile">当前文件是否指向具体的文件</param>
        /// <returns>
        /// 返回包含解析出来信息的键值对
        /// (url:原始url,
        /// serverUrl:包含服务器地址端口和用户名以及密码,
        /// pathUrl:包含路径地址和文件名字,以"/"开头,
        /// host:服务器地址,
        /// port:端口号,
        /// userID:用户名,
        /// pwd:用户密码,
        /// dirPath:当前路径地址,以"/"开头,
        /// fileName:当前指向的文件名字)
        /// </returns>
        public static Hashtable ParseUrl(string url, bool isFile)
        {
            if (string.IsNullOrWhiteSpace(url)) { throw new Exception("ftp连接的url地址不能为空!"); }
            if (url.Length <= 6) { throw new Exception("ftp连接的url地址的长度不满足最小值!"); }
            Hashtable ht = new Hashtable();
            ht.Add("url", url);
            string host = "";
            int port = 21;
            string userID = "";
            string pwd = "";
            string dirPath = "";
            string fileName = "";
            string ftpProto = url.Substring(0, 6);
            //去掉前面的协议头
            if (ftpProto.ToUpper() == "FTP://")
            {
                url = url.Substring(6);
            }
            //截取出来的服务器指向地址
            string serverUrl = "";
            //截取出来的路径指向地址
            string pathUrl = "";
            if (!url.Contains('/'))
            {
                serverUrl = url;
                pathUrl = "/";
            }
            else
            {
                serverUrl = url.Substring(0, url.IndexOf('/'));
                pathUrl = url.Substring(url.IndexOf('/'));
            }
            if (serverUrl.Contains("@"))
            {
                //包含用户名和密码信息
                string userInfo = serverUrl.Substring(0, serverUrl.IndexOf('@'));
                string hostInfo = serverUrl.Substring(serverUrl.IndexOf('@') + 1);
                if (userInfo.Contains(':'))
                {
                    userID = Uri.UnescapeDataString(userInfo.Substring(0, userInfo.IndexOf(':')));
                    pwd = Uri.UnescapeDataString(userInfo.Substring(userInfo.IndexOf(':') + 1));
                }
                else
                {
                    userID = Uri.UnescapeDataString(userInfo);
                }
                if (hostInfo.Contains(':'))
                {
                    host = hostInfo.Substring(0, hostInfo.IndexOf(':'));
                    port = int.Parse(hostInfo.Substring(hostInfo.IndexOf(':') + 1));
                }
                else
                {
                    host = hostInfo;
                }

            }
            else
            {
                //没有用户名和密码信息
                string hostInfo = serverUrl;
                if (hostInfo.Contains(':'))
                {
                    host = hostInfo.Substring(0, hostInfo.IndexOf(':'));
                    port = int.Parse(hostInfo.Substring(hostInfo.IndexOf(':') + 1));
                }
                else
                {
                    host = hostInfo;
                }
            }
            if (isFile)
            {
                //按文件来解析地址
                pathUrl = Uri.UnescapeDataString(pathUrl.Trim('/'));
                if (pathUrl.Contains('/'))
                {
                    dirPath = "/" + pathUrl.Substring(0, pathUrl.LastIndexOf('/'));
                    fileName = pathUrl.Substring(pathUrl.LastIndexOf('/') + 1);
                }
                else
                {
                    dirPath = "/";
                    fileName = pathUrl;
                }

            }
            else
            {
                //按文件夹来解析地址
                dirPath = "/" + Uri.UnescapeDataString(pathUrl.Trim('/'));
                fileName = null;
            }

            ht.Add("serverUrl", serverUrl);
            ht.Add("pathUrl", pathUrl);
            ht.Add("host", host);
            ht.Add("port", port);
            ht.Add("userID", userID);
            ht.Add("pwd", pwd);
            ht.Add("dirPath", dirPath);
            ht.Add("fileName", fileName);
            return ht;
        }
        #endregion

        #region 解析ftp的url地址 public static FtpOP ParseUrl2FtpOP(string url, bool isFile)
        /// <summary>
        /// 解析ftp的url地址(指定当前地址是指向文件夹的还是具体文件的),解析出来服务器地址、端口、用户名、密码、当前所在路径、当前指向的文件的名字
        /// </summary>
        /// <param name="url">要进行解析的url地址</param>
        /// <param name="isFile">当前文件是否指向具体的文件</param>
        /// <returns>返回解析好的FtpOP对象</returns>
        public static FtpOP ParseUrl2FtpOP(string url, bool isFile)
        {
            Hashtable ht = ParseUrl(url, isFile);
            FtpOP op = new FtpOP();
            op.Host = ht["host"] == null ? null : ht["host"].ToString();
            op.Port = ht["port"] == null ? null : ht["port"].ToString();
            op.UserID = ht["userID"] == null ? null : ht["userID"].ToString();
            op.PWD = ht["pwd"] == null ? null : ht["pwd"].ToString();
            op.DirPath = ht["dirPath"] == null ? null : ht["dirPath"].ToString();
            op.FileName = ht["fileName"] == null ? null : ht["fileName"].ToString();
            return op;
        }
        #endregion

        #region 解析ftp的url地址 public static FtpOP ParseUrl2FtpOP(string url, bool isFile,string userID,string pwd)
        /// <summary>
        /// 解析ftp的url地址
        /// </summary>
        /// <param name="url"></param>
        /// <param name="isFile"></param>
        /// <param name="userID"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static FtpOP ParseUrl2FtpOP(string url, bool isFile, string userID, string pwd)
        {
            Hashtable ht = ParseUrl(url, isFile);
            FtpOP op = new FtpOP();
            op.Host = ht["host"] == null ? null : ht["host"].ToString();
            op.Port = ht["port"] == null ? null : ht["port"].ToString();
            op.UserID = userID;
            op.PWD = pwd;
            op.DirPath = ht["dirPath"] == null ? null : ht["dirPath"].ToString();
            op.FileName = ht["fileName"] == null ? null : ht["fileName"].ToString();
            return op;
        }
        #endregion

        #region 关闭连接 public void Close()
        public void Close()
        {
            if (conn != null && conn.IsConnected)
            {
                conn.Close();
            }
        }
        #endregion

        #region 打开连接 public void Open()
        public void Open()
        {
            if (conn == null)
            {
                conn = new FTPConnection();
                conn.ServerAddress = Host;
                conn.ServerPort = int.Parse(Port);
                conn.ServerDirectory = DirPath;
                conn.UserName = UserID;
                conn.Password = PWD;
                conn.Connect();
            }
            if (conn != null && !conn.IsConnected)
            {
                conn.Connect();
            }
        }
        #endregion

        #region 静态方法 快捷操作

        #region 下载文件 public static void DownLoadFile_S(string url,string localFilePath)
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">文件的url地址</param>
        /// <param name="localFilePath">要保存在本地的文件路径</param>
        public static void DownLoadFile_S(string url, string localFilePath)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, true);
            op.DownLoadFile(op.FileName, localFilePath);
        }
        #endregion

        #region 判断是否有指定的多层目录 public static bool HasDirs_S(string url)
        /// <summary>
        /// 判断是否有指定的多层目录(如:a1/a2/a3/a4)
        /// </summary>
        /// <param name="url">ftp路径:如:ftp://administrator:123@127.0.0.1/a1/b1</param>
        /// <returns></returns>
        public static bool HasDirs_S(string url)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, false);
            return op.HasDirs(op.DirPath);
        }
        #endregion

        #region 创建多层次的目录 public static void MakeNonRepeatingDir_S(string url)
        /// <summary>
        /// 创建多层次的目录 如:ftp://administrator:huqinjie123@172.22.210.29/FtpUpload/a1/a2/a3
        /// </summary>
        /// <param name="url"></param>
        public static void MakeNonRepeatingDir_S(string url)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, false);
            op.MakeNonRepeatingDirs(op.DirPath);
        }
        #endregion

        #region 下载目录 public static void DownLoadDir_S(string url, string localDir)
        /// <summary>
        /// 下载指定的目录
        /// </summary>
        /// <param name="url">远程的路径格式如:ftp://administrator:huqingjie123@172.22.210.29</param>
        /// <param name="localDir"></param>
        public static void DownLoadDir_S(string url, string localDir)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, false);
            op.DownLoadDir(op.DirPath, localDir);
        }
        #endregion

        #region 上传目录 public string UploadDir(string localDirPath, string destDirName)
        /// <summary>
        /// 将本机上的目录传送到ftp服务器上指定路径上上,文件会发生覆盖的情况
        /// </summary>
        /// <param name="localDirPath">本地路径如:c:\\temp</param>
        /// <param name="url">ftp路径如:ftp://administrator:123@127.0.0.1:21/FtpUpload/a1</param>
        public static void UploadDir_S(string localDirPath, string url)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, false);
            op.KeepAlive = true;
            op.Open();
            op.UploadDir(localDirPath, op.DirPath);
            op.KeepAlive = false;
            op.Close();
        }
        #endregion

        #region 文件上传 public static void UploadFile_S(string localPath, string url)
        /// <summary>
        /// 将指定的本机文件上传到ftp的指定路径(两种方式)下
        /// </summary>
        /// <param name="localPath">本地文件的绝对路径</param>
        /// <param name="destName">ftp路径如:ftp://administrator:123@127.0.0.1:21/FtpUpload/a1</param>
        public static void UploadFile_S(string localPath, string url)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, false);
            op.UploadFile(localPath, op.DirPath);

        }
        #endregion

        #region 删除目录 public static void DeleteDir_S(string url)
        /// <summary>
        /// 删除目录 
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public static void DeleteDir_S(string url)
        {
            FtpOP op = FtpOP.ParseUrl2FtpOP(url, true);
            op.DeleteDir(op.FileName);
        }
        #endregion
        #endregion

        #region 下载文件 public void DownLoadFile(string fileName,string localFilePath)
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">下载当前路径下的文件,如:"a1/新建文本文档.txt"表示从当前路径下向下索引,"/FtpUpload/a1/新建文本文档.txt"表示从根目录向下索引</param>
        /// <param name="localFilePath"></param>
        public void DownLoadFile(string fileName, string localFilePath)
        {
            FileStream fs = new FileStream(localFilePath, FileMode.Create);
            Open();//打开连接
            conn.DownloadStream(fs, fileName);
            if (!KeepAlive)
            {
                Close();//关闭连接
            }
        }
        #endregion

        #region 获得当前路径(根目录以"/"表示) public string GetCurrentPath()
        /// <summary>
        /// 获得当前路径(根目录以"/"表示)
        /// </summary>
        /// <returns></returns>
        public string GetCurrentPath()
        {
            Open();
            string res = conn.ServerDirectory;
            if (!KeepAlive)
            {
                Close();
            }
            return res;
        }
        #endregion

        #region 创建多层次的目录并且保留当前的工作路径 public void MakeNonRepeatingDirs(string dirName)
        /// <summary>
        /// 创建多层次的目录并且保留当前的工作路径 如:a1/b1/c1或/FtpUpload/a1/b1/c1
        /// </summary>
        /// <param name="dirName">如:a1/b1/c1表示从当前路劲向下索引,/FtpUpload/a1/b1/c1表示从根路径向下索引</param>
        public void MakeNonRepeatingDirs(string dirName)
        {
            Open();
            bool b = KeepAlive;
            KeepAlive = true;//保证在这个函数执行中要打开的状态
            string currentDir = conn.ServerDirectory;
            if (dirName.StartsWith("/"))
            {
                conn.ServerDirectory = "/";
            }
            dirName = dirName.Trim('/');
            string[] names = dirName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < names.Length; i++)
            {
                if (!HasDirs(names[i]))
                {
                    conn.CreateDirectory(names[i]);
                }
                conn.ChangeWorkingDirectory(names[i]);
            }
            conn.ServerDirectory = currentDir;
            KeepAlive = b;
            if (!KeepAlive)
            {
                Close();
            }
        }
        #endregion

        #region 创建多层次的目录 public void MakeNonRepeatingDirAndEnter(string dirName)
        /// <summary>
        /// 创建多层次的目录(如:a1/b1/c1)表示从当前目录向下索引,"/FtpUpload/a1/b2"表示从ftp根目录向下索引
        /// </summary>
        /// <param name="dirName"></param>
        public void MakeNonRepeatingDirAndEnter(string dirName)
        {
            Open();
            bool b = KeepAlive;
            KeepAlive = true;//保证在这个函数执行中要打开的状态
            if (dirName.StartsWith("/"))
            {
                conn.ServerDirectory = "/";
            }
            dirName = dirName.Trim('/');
            string[] names = dirName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < names.Length; i++)
            {
                if (!HasDirs(names[i]))
                {
                    conn.CreateDirectory(names[i]);
                }
                conn.ChangeWorkingDirectory(names[i]);
            }
            if (!KeepAlive)
            {
                Close();
            }
        }
        #endregion

        #region 下载指定的目录 public void DownLoadDir(string dirName, string localDir)
        /// <summary>
        /// 下载ftp指定的目录到本地路径上 可以使用"a1/b1"或"/FtpUpload/a1/b1"形式
        /// </summary>
        /// <param name="dirName">ftp路径(相对当前路径或者是相对根路径)</param>
        /// <param name="localDir">本地路径</param> 
        public void DownLoadDir(string dirName, string localDir)
        {
            KeepAlive = true;
            Open();
            if (!HasDirs(dirName))
            {
                throw new Exception("找不到远程目录:" + conn.ServerDirectory + "/" + dirName);
            }
            string currentDirName = GetCurrentDirName(dirName);
            EnterDirs(dirName);

            if (IsRootPath(localDir))
            {
                //是盘符标识
                localDir = localDir.Substring(0, 1) + ":\\" + Host;
            }
            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }
            localDir = localDir + "/" + currentDirName;
            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }
            FTPFile[] ffs = conn.GetFileInfos();
            foreach (var item in ffs)
            {
                if (!item.Dir)
                {
                    //先处理文件
                    DownLoadFile(item.Name, localDir.Trim('/').Trim('\\') + "/" + item.Name);
                }
                else
                {
                    //再处理子文件夹
                    DownLoadDir(item.Name, localDir);
                    //处理完了之后不要忘记返回上一层
                    conn.ChangeWorkingDirectoryUp();
                }
            }
        }
        #endregion

        #region 判断给定的路径是否是盘符的根节点 private bool IsRootPath(string localDir)
        /// <summary>
        /// 判断给定的路径是否是盘符的根节点
        /// </summary>
        /// <param name="localDir"></param>
        /// <returns></returns>
        private bool IsRootPath(string localDir)
        {
            if (localDir.Contains(":"))
            {
                localDir = localDir.Substring(localDir.LastIndexOf(":") + 1);
                localDir = localDir.Trim('/').Trim('\\');
                if (localDir.Length > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }

            }
            else
            {
                throw new Exception("路径值必须包含\":\"!");
            }
        }
        #endregion

        #region 获得给定路径的当前文件夹的名称 private string GetCurrentDirName(string dirName)
        /// <summary>
        /// 获得给定路径的当前文件夹的名称
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        private string GetCurrentDirName(string dirName)
        {
            dirName = dirName.Trim('/');
            if (dirName.Length == 0)
            {
                return this.Host;
            }
            else
            {
                if (dirName.Contains('/'))
                {
                    return dirName.Substring(dirName.LastIndexOf('/') + 1);
                }
                else
                {
                    return dirName;
                }
            }
        }
        #endregion

        #region 获得当前路径下的所有子目录名字 public List<string> GetSubDirectories()
        /// <summary>
        /// 获得当前路径下的所有子目录名字
        /// </summary>
        /// <returns></returns>
        public List<string> GetSubDirectories()
        {
            Open();
            List<string> list = new List<string>();
            FTPFile[] ffs = conn.GetFileInfos();
            for (int i = 0; i < ffs.Length; i++)
            {
                if (ffs[i].Dir)
                {
                    list.Add(ffs[i].Name);
                }
            }
            if (!KeepAlive)
            {
                Close();
            }
            return list;
        }
        #endregion

        #region 获得当前路径下的文件名字 public List<string> GetFileList()
        /// <summary>
        /// 获得当前路径下的文件名字
        /// </summary>
        /// <returns></returns>
        public List<string> GetFileList()
        {
            Open();
            List<string> list = new List<string>();
            FTPFile[] ffs = conn.GetFileInfos();
            for (int i = 0; i < ffs.Length; i++)
            {
                if (!ffs[i].Dir)
                {
                    list.Add(ffs[i].Name);
                }
            }
            if (!KeepAlive)
            {
                Close();
            }
            return list;
        }
        #endregion

        #region 获得当前路径下的子目录和文件列表 public FTPFile[] GetChildren()
        /// <summary>
        /// 获得当前路径下的子目录和文件列表
        /// </summary>
        /// <returns></returns>
        public FTPFile[] GetChildren()
        {
            Open();
            FTPFile[] ffs = conn.GetFileInfos();
            if (!KeepAlive)
            {
                Close();
            }
            return ffs;
        }
        #endregion

        #region 判断当前路径下是否有指定的多层文件夹(如:a1/b1/c1或/FtpUpload/a1/b1) public bool HasDirs(string dirName)
        /// <summary>
        /// 判断当前路径下是否有指定的多层文件夹(如:a1/b1/c1或/FtpUpload/a1/b1)
        /// </summary>
        /// <param name="dirName">目录的名称,如:"a1/b1"表示从当前路径下向下索引,"/FtpUpload/a1/b1"表示从根路径向下索引</param>
        /// <returns></returns>
        public bool HasDirs(string dirName)
        {
            Open();
            bool b = false;
            if (conn.DirectoryExists(dirName))
            {
                b = true;
            }
            if (!KeepAlive)
            {
                Close();
            }
            return b;
        }
        #endregion

        #region 切换进入当前路径下指定的多层次目录(如:a1/b1/c1或/FtpUpload/a1/b1/c1) public FtpOP EnterDirs(string dirName)
        /// <summary>
        /// 切换进入指定的多层次目录(如:a1/b1/c1或/FtpUpload/a1/b1/c1)
        /// </summary>
        /// <param name="dirName">如:a1/b1/c1或/FtpUpload/a1/b1/c1</param>
        /// <returns></returns>
        public FtpOP EnterDirs(string dirName)
        {
            Open();
            if (dirName.StartsWith("/"))
            {
                conn.ServerDirectory = "/";
            }
            dirName = dirName.Trim('/');
            string[] names = dirName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length < 1) { return this; }

            foreach (var item in names)
            {
                conn.ChangeWorkingDirectory(item);
            }
            if (!KeepAlive)
            {
                Close();
            }
            return this;
        }
        #endregion

        #region 判断当前路径下是否有指定的文件 public bool HasFile(string fileName)
        /// <summary>
        /// 判断当前路径下是否有指定的文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool HasFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) { return false; }
            Open();
            bool b = conn.Exists(fileName);
            if (!KeepAlive)
            {
                Close();
            }
            return b;
        }
        #endregion

        #region 判断当前路径下是否是空的 public bool IsEmpty()
        /// <summary>
        /// 判断当前路径下是否是空的
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            Open();
            bool b = conn.GetFileInfos().Length > 0 ? false : true;
            if (!KeepAlive) { Close(); }
            return b;
        }
        #endregion

        #region 判断当前路径下是否具有文件 public bool HasFiles()
        /// <summary>
        /// 判断当前路径下是否具有文件
        /// </summary>
        /// <returns></returns>
        public bool HasFiles()
        {
            Open();
            bool b = conn.GetFiles().Length > 0 ? true : false;
            if (!KeepAlive) { Close(); }
            return b;
        }
        #endregion

        #region 判断当前路径下是否具有文件夹 public bool HasDirs()
        /// <summary>
        /// 判断当前路径下是否具有文件夹
        /// </summary>
        /// <returns></returns>
        public bool HasDirs()
        {
            Open();
            bool b = conn.GetFileInfos().Count<FTPFile>(i => i.Dir) > 0;
            if (!KeepAlive) { Close(); }
            return b;
        }
        #endregion

        #region 删除当前路径下的指定文件名,如:"a1/b1/c1/tmp.txt"或"/FtpUpload/a1/b1/tmp.txt" public bool DeleteFile(string fileName)
        /// <summary>
        /// 删除指定文件名,如:"a1/b1/c1/tmp.txt"表示删除从当前路径下索引的文件,"/FtpUpload/a1/b1/tmp.txt"表示删除从ftp根目录索引的文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool DeleteFile(string fileName)
        {
            Open();
            bool b = conn.DeleteFile(fileName);
            if (!KeepAlive) { Close(); }
            return b;
        }
        #endregion

        #region 删除当前路径下的指定的多层目录,如:a1/b1/c1 则把当前目录向下延伸把c1目录以及c1目录下的内容全部删除,会保留a1和b1 public bool DeleteDir(string dirName)
        /// <summary>
        /// 删除当前路径下的指定的多层目录,如:a1/b1/c1 删除从当前路径下索引到的c1文件夹,/FtpUpload/a1/b1/c1删除从根路径下索引到的c1文件夹
        /// </summary>
        /// <param name="dirName">如:a1/b1/c1或如:/FtpUpload/a1/b1/c1</param>
        /// <returns></returns>
        public bool DeleteDir(string dirName)
        {
            Open();
            EnterDirs(dirName);
            Open();
            conn.ChangeWorkingDirectoryUp();
            string name = GetCurrentDirName(dirName);
            if (conn.DirectoryExists(name))
            {
                conn.DeleteDirectoryRecursive(name);
            }
            if (!KeepAlive) { Close(); }
            return true;
        }
        #endregion

        #region 清空当前路径下文件和文件夹 public void Clear()
        /// <summary>
        /// 清空当前路径下文件和文件夹(无论文件夹里面有没有文件)
        /// </summary>
        /// <returns></returns>
        public FtpOP Clear()
        {
            Open();
            FTPFile[] ffs = conn.GetFileInfos();
            foreach (var item in ffs)
            {
                if (item.Dir)
                {
                    conn.DeleteDirectoryRecursive(item.Name);
                }
                else
                {
                    conn.DeleteFile(item.Name);
                }
            }
            if (!KeepAlive) { Close(); }
            return this;
        }
        #endregion

        #region 重命名指定文件(两种操作方式,还支持移动文件)  public bool RenameFile(string srcFileName, string destFileName)
        /// <summary>
        /// 重命名指定文件,如:/FtpUpload/a1/新建文本文档.txt-->/FtpUpload/a1/新建文本文档2.txt或a1/tmp.txt-->a1/tmpo.txt
        /// </summary>
        /// <param name="srcFileName">源文件名</param>
        /// <param name="destFileName">目标文件名</param>
        /// <returns></returns>
        public bool RenameFile(string srcFileName, string destFileName)
        {
            Open();
            bool b = false;
            if (conn.Exists(srcFileName))
            {
                b = conn.RenameFile(srcFileName, destFileName);
            }
            if (!KeepAlive) { Close(); }
            return b;
        }
        #endregion

        #region 将指定的本机文件上传到ftp的指定路径(同名则覆盖) public void UploadFile(string localPath)
        /// <summary>
        /// 将指定的本机文件上传到ftp的指定路径(两种方式)下
        /// </summary>
        /// <param name="localPath">本地文件的绝对路径</param>
        /// <param name="destName">目的文件名,支持两种格式(/FtpUpload/a1/yo.txt或yo.txt),为空则表示上传到当前路径下</param>
        public void UploadFile(string localPath, string destName)
        {
            Open();
            string fileName = Path.GetFileName(localPath);
            if (string.IsNullOrWhiteSpace(destName))
            {
                destName = fileName;
            }
            conn.UploadFile(localPath, destName);
            if (!KeepAlive) { Close(); }
        }
        #endregion

        #region 将指定的本机上的多个文件上传到ftp指定路径下(同名则覆盖) public void UploadFiles(List<string> localPaths)
        /// <summary>
        /// 将指定的本机上的多个文件上传到ftp指定路径下(同名则覆盖)
        /// </summary>
        /// <param name="localPaths">本地文件路径的集合</param>
        /// <param name="destDirName">目标路径的名字(两种形式,为null时表示当前路径)</param>
        public void UploadFiles(List<string> localPaths, string destDirName)
        {
            Open();
            string fileName = "";
            foreach (var item in localPaths)
            {
                fileName = Path.GetFileName(item);
                if (!string.IsNullOrWhiteSpace(destDirName))
                {
                    fileName = destDirName.TrimEnd('/').TrimEnd('\\') + "/" + fileName;
                }
                conn.UploadFile(item, fileName);
            }
            if (!KeepAlive) { Close(); }
        }
        #endregion

        #region 获取指定文件的大小(如果没有发现文件则返回-1) public long GetFileSize(string fileName)
        /// <summary>
        /// 获取指定文件的大小 两种路径选择方式
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public long GetFileSize(string fileName)
        {
            Open();
            string dirName = GetDirName(fileName);
            fileName = GetFileName(fileName);
            conn.ServerDirectory = dirName;
            FTPFile[] ffs = conn.GetFileInfos();
            foreach (var item in ffs)
            {
                if (!item.Dir && item.Name == fileName)
                {
                    if (!KeepAlive) { Close(); }
                    return item.Size;
                }
            }
            if (!KeepAlive) { Close(); }
            return -1;
        }
        #endregion

        #region 获得给定的路径所对应的文件名字 private string GetFileName(string fileName)
        /// <summary>
        /// 获得给定的路径所对应的文件名字
        /// </summary>
        /// <param name="fileName">如:/FtpUpload/a1/tmpo.txt或a1/tmpo.txt</param>
        /// <returns></returns>
        private string GetFileName(string fileName)
        {
            fileName = fileName.Trim('/').Trim('\\');
            if (fileName.Contains('/'))
            {
                string[] names = fileName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                return names[names.Length - 1];
            }
            else
            {
                return fileName;
            }
        }
        #endregion

        #region 获得给定路径所对应的文件夹的名字如:/FtpUpload/a1/to.txt返回/ftpUpload/a1 private string GetDirName(string fileName)
        /// <summary>
        /// 获得给定路径所对应的文件夹的名字如:/FtpUpload/a1/to.txt返回/ftpUpload/a1
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetDirName(string fileName)
        {
            string res = "";
            if (fileName.StartsWith("/"))
            {
                res = "/";
            }
            fileName = fileName.Trim('/').Trim('\\');
            if (fileName.Contains('/'))
            {
                res += fileName.Substring(0, fileName.LastIndexOf('/'));
            }
            return res;
        }
        #endregion

        #region 将本机上的目录传送到ftp服务器上指定路径上上,文件会发生覆盖的情况 public string UploadDir(string localDirPath, string destDirName)
        /// <summary>
        /// 将本机上的目录传送到ftp服务器上的当前目录上,文件会发生覆盖的情况(暂不支持直接将整个盘符里的内容上传)
        /// 目标路径如:/FtpUpload/test3或test3
        /// </summary>
        /// <param name="localDirPath">本地路径如:c:/tmp</param>
        /// <param name="destDirName">目标路径如:/FtpUpload/test3或test3</param>
        /// <returns></returns>
        public string UploadDir(string localDirPath, string destDirName)
        {
            localDirPath = localDirPath.Replace('\\', '/');
            if (Regex.IsMatch(localDirPath, "^[a-z|A-Z]:/{0,}$"))
            {
                throw new Exception("不支持盘符上传!");
            }
            Open();
            KeepAlive = true;
            string dirName = GetCurrentDirName(localDirPath);
            MakeNonRepeatingDirAndEnter(destDirName);
            if (!conn.DirectoryExists(dirName))
            {
                conn.CreateDirectory(dirName);
            }
            UploadDirRecursive(localDirPath, dirName);
            //if (!conn.DirectoryExists(destDirName))
            //{
            //    MakeNonRepeatingDirs(destDirName);                
            //}
            //string rootDirName = string.IsNullOrWhiteSpace(destDirName)?conn.ServerDirectory:destDirName;
            //UploadDirRecursive(localDirPath, ref rootDirName);
            //KeepAlive = b;
            if (!KeepAlive) { Close(); }
            //return rootDirName;
            return "";
        }
        #endregion

        #region 递归上传目录方法 public void UploadDirRecursive(string localDirPath, string destDirName)
        /// <summary>
        /// 递归上传目录方法 
        /// </summary>
        /// <param name="localDirPath"></param>
        /// <param name="destDirName"></param>
        private void UploadDirRecursive(string localDirPath, string destDirName)
        {
            localDirPath = localDirPath.Replace('\\', '/');
            MakeNonRepeatingDirAndEnter(destDirName);
            string dirName = GetCurrentDirName(localDirPath);
            DirectoryInfo dinfo = new DirectoryInfo(localDirPath);
            FileInfo[] finfos = dinfo.GetFiles();
            foreach (var item in finfos)
            {
                conn.UploadFile(item.FullName, item.Name);
            }
            DirectoryInfo[] dinfos = dinfo.GetDirectories();
            foreach (var item in dinfos)
            {
                UploadDirRecursive(item.FullName, item.Name);
                conn.ChangeWorkingDirectoryUp();
            }
        }
        #endregion
    }
    public static class FtpClientExtensions
    {
        #region 递归获取目录的所有子目录和文件信息 public static FTPFile[] GetFileInfosRecursive(this FTPConnection conn)
        /// <summary>
        /// 递归获取目录的所有子目录和文件信息
        /// </summary>
        public static FTPFile[] GetFileInfosRecursive(this FTPConnection conn)
        {
            var resultList = new List<FTPFile>();
            var fileInfos = conn.GetFileInfos();
            resultList.AddRange(fileInfos);
            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.Dir)
                {
                    conn.ServerDirectory = fileInfo.Path;
                    resultList.AddRange(conn.GetFileInfosRecursive());
                }
            }
            return resultList.ToArray();
        }
        #endregion

        #region 递归删除目录(包括所有子目录和文件信息) public static void DeleteDirectoryRecursive(this FTPConnection conn, string directoryName)
        /// <summary>
        /// 递归删除目录(包括所有子目录和文件信息)
        /// </summary>
        public static void DeleteDirectoryRecursive(this FTPConnection conn, string directoryName)
        {
            conn.ChangeWorkingDirectory(directoryName);
            var fileInfos = conn.GetFileInfos();
            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.Dir)
                    conn.DeleteDirectoryRecursive(fileInfo.Name);
                else
                    conn.DeleteFile(fileInfo.Name);
            }
            conn.ChangeWorkingDirectoryUp();
            conn.DeleteDirectory(directoryName);
        }
        #endregion
    }
}