using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nightfire_Source_Updater_Client
{
    class Utils
    {
        static public double roundToMb(long number)
        {
            return Math.Round(((number * 1.0) / 1000 / 1024), 2);
        }

        //https://stackoverflow.com/questions/924679/c-sharp-how-can-i-check-if-a-url-exists-is-valid
        static public bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will return false.
                return false;
            }
        }
    }
}
