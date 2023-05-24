using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Data;
using System.Data.SQLite;
using Dapper;
using System.Net;

namespace BrowserCookiesGrabber.Chromium
{
    public class ChromiumCookiesGrabber
    {
        private readonly string _userChromiumCookiesPath;
        private readonly string _userLocalStatePath;

        private readonly byte[] _aesKey;

        private readonly BCrypt.SafeAlgorithmHandle hAlg;
        private readonly BCrypt.SafeKeyHandle hKey;

        private const int AES_BLOCK_SIZE = 16;
        private static readonly byte[] DPAPI_HEADER = Encoding.UTF8.GetBytes("DPAPI");
        private static readonly byte[] DPAPI_CHROME_UNKV10 = Encoding.UTF8.GetBytes("v10");

        public ChromiumCookiesGrabber(string basePath, string cookiesFilePath)
        {
            if (Environment.GetEnvironmentVariable("USERNAME").Contains("SYSTEM"))
            {
                throw new Exception("Cannot decrypt Chromium credentials from a SYSTEM level context.");
            }

            _userChromiumCookiesPath = cookiesFilePath;
            _userLocalStatePath = Path.Combine(basePath, "Local State");

            var base64Key = GetBase64EncryptedKey();
            if (base64Key == string.Empty)
            {
                throw new Exception("Could not retrieve deceryption key");
            }

            _aesKey = DecryptBase64StateKey(base64Key);
            if (_aesKey == null)
            {
                throw new Exception("Failed to decrypt AES Key.");
            }

            DPAPIChromiumAlgFromKeyRaw(_aesKey, out hAlg, out hKey);
            if (hAlg == null || hKey == null)
            {
                throw new Exception("Failed to create BCrypt Symmetric Key.");
            }
        }

        public async Task<CookieContainer> GetCookiesAsync(string? domain = null)
        {
            var cookiePath = Path.GetTempFileName();
            File.Copy(_userChromiumCookiesPath, cookiePath, overwrite: true);

            IEnumerable<dynamic> rows;
            try
            {
                using var sqlConnection = new SQLiteConnection($"Data Source={cookiePath}");

                if (!string.IsNullOrEmpty(domain))
                {
                    rows = await sqlConnection.QueryAsync($"select * from cookies where host_key = @hostKey", new { hostKey = domain });
                }
                else
                {
                    rows = await sqlConnection.QueryAsync($"select * from cookies");
                }
            }
            finally
            {
                File.Delete(cookiePath);
            }

            var dictRows = rows.Cast<IDictionary<string, object>>().AsList();
            var rawCookies = ExtractCookiesFromSQLQuery(dictRows);

            return rawCookies;
        }

        private CookieContainer ExtractCookiesFromSQLQuery(List<IDictionary<string, object>> rows)
        {
            var cookies = new CookieContainer();

            foreach (var row in rows)
            {
                var cookie = new Cookie
                {
                    Domain = row["host_key"].ToString(),
                    Name = row["name"].ToString(),
                    Path = row["path"].ToString(),
                    HttpOnly = row["is_httponly"].ToString() == "1",
                    Secure = row["is_secure"].ToString() == "1",
                    Expires = Convert.ToInt64(row["expires_utc"]).ExpiresUtcEpochToDateTime(),
                };

                // Value

                byte[] cookieValue = (byte[])row["encrypted_value"];
                cookieValue = DecryptBlob(cookieValue);
                if (cookieValue != null)
                {
                    cookie.Value = Encoding.UTF8.GetString(cookieValue);
                }
                else
                {
                    cookie.Value = string.Empty;
                }

                cookies.Add(cookie);
            }
            return cookies;
        }

        private byte[]? DecryptBlob(byte[] dwData)
        {
            //if (hKey == null && hAlg == null)
            //{
            //    return ProtectedData.Unprotect(dwData, null, DataProtectionScope.CurrentUser);
            //}
            byte[]? dwDataOut = null;
            int dwDataOutLen;
            //IntPtr pDataOut = IntPtr.Zero;
            IntPtr pData = IntPtr.Zero;
            uint ntStatus;
            byte[] subArrayNoV10;
            int pcbResult = 0;
            unsafe
            {
                if (ByteArrayEquals(dwData, 0, DPAPI_CHROME_UNKV10, 0, 3))
                {
                    subArrayNoV10 = new byte[dwData.Length - DPAPI_CHROME_UNKV10.Length];
                    Array.Copy(dwData, 3, subArrayNoV10, 0, dwData.Length - DPAPI_CHROME_UNKV10.Length);
                    pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * dwData.Length);
                    //byte[] shiftedEncVal = new byte[dwData.Length - 3];
                    //Array.Copy(dwData, 3, shiftedEncVal, 0, dwData.Length - 3);
                    //IntPtr shiftedEncValPtr = IntPtr.Zero;
                    try
                    {

                        //shiftedEncValPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * shiftedEncVal.Length);
                        Marshal.Copy(dwData, 0, pData, dwData.Length);
                        // magic decryption happens here
                        BCrypt.BCRYPT_INIT_AUTH_MODE_INFO(out var authModeInfo);
                        authModeInfo.pbNonce = (byte*)(pData + DPAPI_CHROME_UNKV10.Length);
                        authModeInfo.cbNonce = 12;
                        authModeInfo.pbTag = authModeInfo.pbNonce + dwData.Length - (DPAPI_CHROME_UNKV10.Length + AES_BLOCK_SIZE); // AES_BLOCK_SIZE = 16
                        authModeInfo.cbTag = AES_BLOCK_SIZE; // AES_BLOCK_SIZE = 16

                        dwDataOutLen = dwData.Length - DPAPI_CHROME_UNKV10.Length - authModeInfo.cbNonce - authModeInfo.cbTag;
                        dwDataOut = new byte[dwDataOutLen];

                        fixed (byte* pDataOut = dwDataOut)
                        {
                            ntStatus = BCrypt.BCryptDecrypt(hKey, authModeInfo.pbNonce + authModeInfo.cbNonce, dwDataOutLen, &authModeInfo, null, 0, pDataOut, dwDataOutLen, out pcbResult, 0);
                        }
                        //if (NT_SUCCESS(ntStatus))
                        //{
                        //    //Console.WriteLine("{0} : {1}", dwDataOutLen, pDataOut);
                        //}
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (/*pData != null && */pData != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pData);
                        }
                        //if (pDataOut != null && pDataOut != IntPtr.Zero)
                        //    Marshal.FreeHGlobal(pDataOut);
                        //if (pInfo != null && pInfo != IntPtr.Zero)
                        //    Marshal.FreeHGlobal(pDataOut);
                    }
                }
            }
            return dwDataOut;
        }



        public static byte[] DecryptBase64StateKey(string base64Key)
        {
            byte[] encryptedKeyBytes = Convert.FromBase64String(base64Key);
            if (ByteArrayEquals(DPAPI_HEADER, 0, encryptedKeyBytes, 0, 5))
            {
                //Console.WriteLine("> Key appears to be encrypted using DPAPI");
                byte[] encryptedKey = new byte[encryptedKeyBytes.Length - 5];
                Array.Copy(encryptedKeyBytes, 5, encryptedKey, 0, encryptedKeyBytes.Length - 5);
                byte[] decryptedKey = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
                return decryptedKey;
            }
            else
            {
                Console.WriteLine("Unknown encoding.");
            }
            return null;
        }

        private static bool ByteArrayEquals(byte[] sourceArray, int sourceIndex, byte[] destArray, int destIndex, int len)
        {
            int j = destIndex;
            for (int i = sourceIndex; i < sourceIndex + len; i++)
            {
                if (sourceArray[i] != destArray[j])
                    return false;
                j++;
            }
            return true;
        }

        public string GetBase64EncryptedKey()
        {
            var localStateData = File.ReadAllText(_userLocalStatePath);

            var searchTerm = "encrypted_key";
            int startIndex = localStateData.IndexOf(searchTerm);
            if (startIndex < 0)
            {
                return string.Empty;
            }

            // encrypted_key":"BASE64"
            var keyIndex = startIndex + searchTerm.Length + 3;
            var tempVals = localStateData.Substring(keyIndex);

            var stopIndex = tempVals.IndexOf('"');
            if (stopIndex < 0)
            {
                return "";
            }

            var base64Key = tempVals.Substring(0, stopIndex);
            return base64Key;
        }

        private static bool NT_SUCCESS(uint status)
        {
            return 0 == status;
        }

        //kuhl_m_dpapi_chrome_alg_key_from_raw
        public static bool DPAPIChromiumAlgFromKeyRaw(byte[] key, out BCrypt.SafeAlgorithmHandle hAlg, out BCrypt.SafeKeyHandle? hKey)
        {
            var bRet = false;
            hKey = null;

            var ntStatus = BCrypt.BCryptOpenAlgorithmProvider(out hAlg, "AES", null!, 0);
            if (NT_SUCCESS(ntStatus))
            {
                ntStatus = BCrypt.BCryptSetProperty(hAlg, "ChainingMode", "ChainingModeGCM", 0);
                if (NT_SUCCESS(ntStatus))
                {
                    ntStatus = BCrypt.BCryptGenerateSymmetricKey(hAlg, out hKey, null!, 0, key, key.Length, 0);
                    if (NT_SUCCESS(ntStatus))
                    {
                        bRet = true;
                    }
                }
            }
            return bRet;
        }
    }
}
