﻿// Copyright 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VsChromium.Core.Ipc.TypedMessages;
using VsChromium.Core.Linq;
using VsChromium.ServerProxy;
using VsChromium.Tests.Server;

namespace VsChromium.Tests.ServerProcess {
  [TestClass]
  public class TestSearchFilePaths : TestServerBase {
    private static CompositionContainer _container;
    private static ITypedRequestProcessProxy _server;
    private static FileInfo _testFile;

    [ClassInitialize]
    public static void Initialize(TestContext context) {
      _container = SetupMefContainer();
      _server = _container.GetExportedValue<ITypedRequestProcessProxy>();
      _testFile = Utils.GetChromiumTestEnlistmentFile();
      GetFileSystemFromServer(_server, _testFile);
    }

    [ClassCleanup]
    public static void Cleanup() {
      _server.Dispose();
      _container.Dispose();
    }

    [TestMethod]
    public void SingleOccurrenceWorks() {
      VerifySearchFilePathsResponse(_server, _testFile.Name, _testFile.Directory, _testFile.Name, 1);
    }

    [TestMethod]
    public void MultipleOccurrenceWorks() {
      const string fileName = "file_present_three_times.txt";
      const string searchPattern = "file_present_three_times.txt";

      VerifySearchFilePathsResponse(_server, searchPattern, _testFile.Directory, fileName, 3);
    }

    [TestMethod]
    public void WildcardWorks() {
      const string fileName = "file_present_three_times.txt";
      const string searchPattern = "file_present_*_times.*";

      VerifySearchFilePathsResponse(_server, searchPattern, _testFile.Directory, fileName, 3);
    }

    [TestMethod]
    public void SemiColonSeparatorWithWildcardWorks() {
      const string searchPattern = "*.txt;*.py";
      const string fileName = "";

      VerifySearchFilePathsResponse(_server, searchPattern, _testFile.Directory, fileName, 9);
    }

    [TestMethod]
    public void SemiColonSeparatorWithPartialNamesWorks() {
      const string searchPattern = ".txt;.py";
      const string fileName = "";

      VerifySearchFilePathsResponse(_server, searchPattern, _testFile.Directory, fileName, 9);
    }

    private static void VerifySearchFilePathsResponse(
      ITypedRequestProcessProxy server,
      string searchPattern,
      DirectoryInfo chromiumDirectory,
      string fileName,
      int occurrenceCount) {
      var response = SendRequest<SearchFilePathsResponse>(server, new SearchFilePathsRequest {
        SearchParams = new SearchParams {
          SearchString = searchPattern,
          MaxResults = 2000,
        }
      }, ServerResponseTimeout)();
      Assert.IsNotNull(response, "Server did not respond within timeout.");
      Assert.IsNotNull(response.SearchResult);
      Assert.IsNotNull(response.SearchResult.Entries);

      Assert.AreEqual(1, response.SearchResult.Entries.Count);
      var chromiumEntry = response.SearchResult.Entries[0] as DirectoryEntry;
      Assert.IsNotNull(chromiumEntry);
      Assert.AreEqual(chromiumDirectory.FullName, chromiumEntry.Name);

      chromiumEntry.Entries.ForAll(x => Debug.WriteLine(string.Format("File name: \"{0}\"", x.Name)));
      Assert.AreEqual(occurrenceCount, chromiumEntry.Entries.Count);
      if (fileName != "") {
        Assert.AreEqual(occurrenceCount, chromiumEntry.Entries.Count(x => Path.GetFileName(x.Name) == fileName));
      }
    }
  }
}
