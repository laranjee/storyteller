﻿using System;
using System.Collections.Generic;
using FubuCore;
using StoryTeller.Assertions;
using StoryTeller.Domain;
using StoryTeller.Engine;
using StoryTeller.Model;
using StoryTeller.Workspace;

namespace StoryTeller.Execution
{
    public class ProjectTestRunner : IDisposable
    {
        private IProject _project;
        private TestEngine _engine;
        private Hierarchy _hierarchy;
        private OpenHtmlOption _openOption = OpenHtmlOption.Always;

        public ProjectTestRunner(string projectFile)
        {
            LoadProject(projectFile);
        }

        public ResultHistory GetResults()
        {
            return _hierarchy.GetFullResults();
        }

        public void LoadProject(string projectFile)
        {
            if (_project != null)
            {
                Dispose();
            }

            _project = StoryTeller.Workspace.Project.LoadFromFile(projectFile);
            _engine = new TestEngine();
            _engine.Handle(new ProjectLoaded(_project));


            _hierarchy = _project.LoadTests();
        }

        public Hierarchy Hierarchy
        {
            get { return _hierarchy; }
        }

        public IProject Project
        {
            get { return _project; }
        }

        public TestStopConditions StopConditions
        {
            get { return _engine.StopConditions; }
        }

        public Test RunTest(string testPath)
        {
            Test test = FindTest(testPath);

            _engine.RunTest(test);


            return test;
        }

        public Test FindTest(string testPath)
        {
            var test = _hierarchy.FindTest(testPath);

            if (test == null) throw new ArgumentOutOfRangeException("Test {0} cannot be found".ToFormat(testPath));
            return test;
        }

        public void RunAndAssertTest(string testPath)
        {
            var test = RunTest(testPath);

            bool shouldOpen = shouldOpenTest(test.LastResult);
            if (shouldOpen)
            {
                test.OpenResultsInBrowser();
            }

            if (!test.WasSuccessful())
            {
                throw new StorytellerAssertionException(test.GetStatus());
            }
        }

        private bool shouldOpenTest(TestResult testResult)
        {
            switch (_openOption)
            {
                case (OpenHtmlOption.Always):
                    return true;

                case (OpenHtmlOption.FailureOnly):
                    return !testResult.Counts.WasSuccessful();

                case (OpenHtmlOption.Never):
                    return false;
            }

            return false;
        }

        public void Dispose()
        {
            _engine.Dispose();
            _engine = null;
        }

        public FixtureLibrary GetLibary()
        {
            return _engine.Library;
        }

        public void RunAll()
        {
            _hierarchy.GetAllTests().Each(t => _engine.RunTest(t));
        }
    }
}