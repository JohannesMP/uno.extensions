﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Locations;
using Microsoft.Templates.Test;

namespace TemplateStudioForWinUICpp.Tests
{
    public class WinUICppBaseGenAndBuildTests : BaseGenAndBuildTests
    {
        public WinUICppBaseGenAndBuildTests(BaseGenAndBuildFixture fixture, IContextProvider contextProvider = null, string framework = "")
            : base(fixture, contextProvider, framework)
        {
        }

        // Set a single programming language to stop the fixture using all languages available to it
        public static new IEnumerable<object[]> GetProjectTemplatesForBuild(string framework, string programmingLanguage, string platform)
        {
            List<object[]> result = new List<object[]>();

            if (string.IsNullOrWhiteSpace(framework))
            {
                foreach (var fwork in Frameworks.All)
                {
                    result.AddRange(BuildTemplatesTestFixture.GetProjectTemplates(new WinUICppTestsTemplatesSource(), fwork, programmingLanguage, platform));
                }
            }
            else
            {
                result = BuildTemplatesTestFixture.GetProjectTemplates(new WinUICppTestsTemplatesSource(), framework, programmingLanguage, platform).ToList();
            }

            return result;
        }

        public static new IEnumerable<object[]> GetPageAndFeatureTemplatesForBuild(string framework, string language, string platform, string excludedItem = "")
        {
            return WinUICppBuildTemplatesTestFixture.GetPageAndFeatureTemplatesForBuild(framework, language, platform, excludedItem);
        }
    }
}
