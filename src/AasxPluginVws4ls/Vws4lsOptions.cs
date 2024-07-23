/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginVws4ls
{
    public class Vws4lsOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        private string TemplateIdAsset = "/ids/asset/DDDD_DDDD_DDDD_DDDD";
        private string TemplateIdAas = "/ids/aas/DDDD_DDDD_DDDD_DDDD";
        private string TemplateIdSubmodel = "/ids/submodel/DDDD_DDDD_DDDD_DDDD";
        public Dictionary<string, string> AssetIdByPartNumberDict = new Dictionary<string, string>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static Vws4lsOptions CreateDefault()
        {
            var opt = new Vws4lsOptions();
            return opt;
        }

        public string GetTemplateIdAsset(string hostName)
        {
            return GetNormalizedHostName(hostName) + TemplateIdAsset;
        }

        public string GetTemplateIdAas(string hostName)
        {
            return GetNormalizedHostName(hostName) + TemplateIdAas;
        }

        public string GetTemplateIdSubmodel(string hostName)
        {
            return GetNormalizedHostName(hostName) + TemplateIdSubmodel;
        }

        private string GetNormalizedHostName(string hostName)
        {
            return (hostName ?? "https://www.example.com").TrimEnd('/');
        }
    }
}
