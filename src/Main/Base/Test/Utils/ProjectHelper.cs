﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Project;
using Rhino.Mocks;

namespace ICSharpCode.SharpDevelop.Tests.Utils
{
	public class ProjectHelper
	{
		public IProject Project = MockRepository.GenerateMock<IProject, IBuildable>();
		
		public ProjectHelper(string fileName)
		{
			Project.Stub(p => p.FileName).Return(FileName.Create(fileName));
			
			Project.Stub(p => p.Items).Return(new SimpleModelCollection<ProjectItem>());
			
			Project.Stub(p => p.Preferences).Return(new Properties());
			
			Project.Stub(p => p.SyncRoot).Return(new Object());
		}
	}
}
