﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.IO;
using System.Configuration;

using Site;
using ZD.Common;
using ZD.CedictEngine;

namespace Site
{
    public class Global : HttpApplication
    {
        private static ICedictEngine dict;

        public static ICedictEngine Dict
        {
            get { return dict; }
        }

        private static string gaCode;
        
        public static string GACode
        {
            get { return gaCode; }
        }

        void Application_Start(object sender, EventArgs e)
        {
            // Load dictionary
            string dictFilePath = HttpRuntime.AppDomainAppPath;
            dictFilePath = Path.Combine(dictFilePath, "_data");
            dictFilePath = Path.Combine(dictFilePath, "handedict-zydeo.bin");
            dict = new DictEngine(dictFilePath, new FontCoverageFull());

            // Initialize text provider
            TextProvider.Init();

            // Some static config parameters
            AppSettingsReader asr = new AppSettingsReader();
            gaCode = asr.GetValue("gaCode", typeof(string)).ToString();
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }
    }
}
