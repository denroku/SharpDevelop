﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.Core;
using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using ICSharpCode.SharpDevelop.Project;
using NuGet;
using NUnit.Framework;
using Rhino.Mocks;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests
{
	[TestFixture]
	public class SharpDevelopPackageManagerTests
	{
		SharpDevelopPackageManager packageManager;
		FakePackageRepository fakeFeedSourceRepository;
		FakeSharedPackageRepository fakeSolutionSharedRepository;
		IProject testProject;
		PackageManagementOptions options;
		SolutionPackageRepositoryPath repositoryPaths;
		PackageReferenceRepositoryHelper packageRefRepositoryHelper;
		TestableProjectManager testableProjectManager;
		FakeFileSystem fakeFileSystem;
		FakePackageOperationResolverFactory fakePackageOperationResolverFactory;
		IPackageOperationResolver fakePackageOperationResolver;
		
		void CreatePackageManager(IProject project, PackageReferenceRepositoryHelper packageRefRepositoryHelper)
		{
			options = new TestablePackageManagementOptions();
			options.PackagesDirectory = "packages";
			
			repositoryPaths = new SolutionPackageRepositoryPath(project, options);
			var pathResolver = new DefaultPackagePathResolver(repositoryPaths.PackageRepositoryPath);
			
			fakeFileSystem = new FakeFileSystem();
			
			fakeFeedSourceRepository = new FakePackageRepository();
			fakeSolutionSharedRepository = packageRefRepositoryHelper.FakeSharedSourceRepository;
			
			fakePackageOperationResolverFactory = new FakePackageOperationResolverFactory();
			
			var fakeSolutionPackageRepository = new FakeSolutionPackageRepository();
			fakeSolutionPackageRepository.FileSystem = fakeFileSystem;
			fakeSolutionPackageRepository.PackagePathResolver = pathResolver;
			fakeSolutionPackageRepository.FakeSharedRepository = fakeSolutionSharedRepository;
			
			packageManager = new SharpDevelopPackageManager(fakeFeedSourceRepository,
				packageRefRepositoryHelper.FakeProjectSystem,
				fakeSolutionPackageRepository,
				fakePackageOperationResolverFactory);
		}
		
		void CreatePackageManager()
		{
			CreateTestProject();
			CreatePackageReferenceRepositoryHelper();
			CreatePackageManager(testProject, packageRefRepositoryHelper);
		}
		
		void CreatePackageReferenceRepositoryHelper()
		{
			packageRefRepositoryHelper = new PackageReferenceRepositoryHelper();
		}
		
		void CreateTestProject()
		{
			testProject = ProjectHelper.CreateTestProject();
			testProject.ParentSolution.Stub(s => s.Directory).Return(DirectoryName.Create(@"c:\projects\Test\MyProject"));
		}
		
		void CreateTestableProjectManager()
		{
			testableProjectManager = new TestableProjectManager();
			packageManager.ProjectManager = testableProjectManager;
		}
		
		FakePackage CreateFakePackage(string id = "Test", string version = "1.0.0.0")
		{
			return new FakePackage(id, version);
		}
		
		FakePackage InstallPackage()
		{
			FakePackage package = CreateFakePackage();
			packageManager.InstallPackage(package);
			return package;
		}
		
		FakePackage InstallPackageWithNoPackageOperations()
		{
			return InstallPackageWithNoPackageOperations(ignoreDependencies: false);
		}
		
		FakePackage InstallPackageWithNoPackageOperationsAndIgnoreDependencies()
		{
			return InstallPackageWithNoPackageOperations(ignoreDependencies: true);
		}
		
		FakePackage InstallPackageWithNoPackageOperationsAndAllowPrereleaseVersions()
		{
			return InstallPackageWithNoPackageOperations(ignoreDependencies: false, allowPrereleaseVersions: true);
		}
		
		FakePackage InstallPackageWithNoPackageOperations(bool ignoreDependencies)
		{
			return InstallPackageWithNoPackageOperations(ignoreDependencies, allowPrereleaseVersions: false);
		}
		
		FakePackage InstallPackageWithNoPackageOperations(bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			FakePackage package = CreateFakePackage();
			var operations = new List<PackageOperation>();
			var installAction = new FakeInstallPackageAction();
			installAction.IgnoreDependencies = ignoreDependencies;
			installAction.AllowPrereleaseVersions = allowPrereleaseVersions;
			installAction.Operations = operations;
			packageManager.InstallPackage(package, installAction);
			return package;
		}
		
		FakePackage InstallPackageWithPackageOperations(PackageOperation operation)
		{
			var operations = new PackageOperation[] {
				operation
			};
			FakePackage package = CreateFakePackage();
			var installAction = new FakeInstallPackageAction();
			installAction.Operations = operations;
			packageManager.InstallPackage(package, installAction);
			return package;
		}
		
		FakePackage InstallPackageAndIgnoreDependencies()
		{
			return InstallPackageWithParameters(true, false);
		}
		
		FakePackage InstallPackageWithParameters(bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			FakePackage package = CreateFakePackage();
			packageManager.InstallPackage(package, ignoreDependencies, allowPrereleaseVersions);
			return package;
		}
		
		FakePackage InstallPackageAndAllowPrereleaseVersions()
		{
			return InstallPackageWithParameters(false, true);
		}
		
		FakePackage InstallPackageAndDoNotAllowPrereleaseVersions()
		{
			return InstallPackageWithParameters(false, false);
		}
		
		FakePackage InstallPackageAndDoNotIgnoreDependencies()
		{
			return InstallPackageWithParameters(false, false);
		}
		
		FakePackage UninstallPackage()
		{
			FakePackage package = CreateFakePackage();
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			
			packageManager.UninstallPackage(package);
			return package;
		}

		FakePackage UninstallPackageAndForceRemove()
		{
			FakePackage package = CreateFakePackage();
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			
			bool removeDependencies = false;
			bool forceRemove = true;
			packageManager.UninstallPackage(package, forceRemove, removeDependencies);
			
			return package;
		}
		
		FakePackage UninstallPackageAndRemoveDependencies()
		{
			FakePackage package = CreateFakePackage();
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			
			bool removeDependencies = true;
			bool forceRemove = false;
			packageManager.UninstallPackage(package, forceRemove, removeDependencies);
			
			return package;
		}
		
		PackageOperation CreateOneInstallPackageOperation(string id = "PackageToInstall", string version = "1.0")
		{
			FakePackage package = CreateFakePackage(id, version);
			return new PackageOperation(package, PackageAction.Install);
		}
		
		IEnumerable<PackageOperation> GetInstallPackageOperations(FakePackage package)
		{
			return GetInstallPackageOperations(package, false, false);
		}
		
		IEnumerable<PackageOperation> GetInstallPackageOperationsAndIgnoreDependencies(FakePackage package)
		{
			return GetInstallPackageOperations(package, true, false);
		}
		
		IEnumerable<PackageOperation> GetInstallPackageOperationsAndAllowPrereleaseVersions(FakePackage package)
		{
			return GetInstallPackageOperations(package, false, true);
		}
		
		IEnumerable<PackageOperation> GetInstallPackageOperations(
			FakePackage package,
			bool ignoreDependencies,
			bool allowPrereleaseVersions)
		{
			var fakeInstallAction = new FakeInstallPackageAction();
			fakeInstallAction.IgnoreDependencies = ignoreDependencies;
			fakeInstallAction.AllowPrereleaseVersions = allowPrereleaseVersions;
			return packageManager.GetInstallPackageOperations(package, fakeInstallAction);
		}
		
		FakePackage UpdatePackageWithNoPackageOperations()
		{
			FakePackage package = CreateFakePackage();
			var updateAction = new FakeUpdatePackageAction();
			updateAction.Operations = new List<PackageOperation>();
			updateAction.UpdateDependencies = true;
			packageManager.UpdatePackage(package, updateAction);
			return package;
		}
		
		FakePackage UpdatePackageWithPackageOperations(PackageOperation operation)
		{
			var operations = new PackageOperation[] {
				operation
			};
			FakePackage package = CreateFakePackage();
			var updateAction = new FakeUpdatePackageAction();
			updateAction.Operations = operations;
			updateAction.UpdateDependencies = true;
			packageManager.UpdatePackage(package, updateAction);
			return package;
		}
		
		FakePackage UpdatePackageWithNoPackageOperationsAndDoNotUpdateDependencies()
		{
			return UpdatePackageWithNoPackageOperations(false, false);
		}
		
		FakePackage UpdatePackageWithNoPackageOperationsAndAllowPrereleaseVersions()
		{
			return UpdatePackageWithNoPackageOperations(false, true);
		}
		
		FakePackage UpdatePackageWithNoPackageOperations(bool updateDependencies, bool allowPrereleaseVersions)
		{
			FakePackage package = CreateFakePackage();
			var updateAction = new FakeUpdatePackageAction();
			updateAction.Operations = new List<PackageOperation>();
			updateAction.UpdateDependencies = updateDependencies;
			updateAction.AllowPrereleaseVersions = allowPrereleaseVersions;
			packageManager.UpdatePackage(package, updateAction);
			return package;
		}
		
		UpdatePackagesAction CreateUpdatePackagesAction()
		{
			return new UpdatePackagesAction(new FakePackageManagementProject(), null);
		}
		
		UpdatePackagesAction CreateUpdatePackagesActionWithPackages(params IPackageFromRepository[] packages)
		{
			UpdatePackagesAction action = CreateUpdatePackagesAction();
			action.AddPackages(packages);
			return action;
		}
		
		UpdatePackagesAction CreateUpdatePackagesActionWithOperations(params PackageOperation[] operations)
		{
			UpdatePackagesAction action = CreateUpdatePackagesAction();
			action.AddOperations(operations);
			return action;
		}
		
		PackageOperation AddInstallOperationForPackage(IPackage package)
		{
			var operation = new PackageOperation(package, PackageAction.Install);
			AddInstallOperationsForPackage(package, operation);
			return operation;
		}
		
		void AddInstallOperationsForPackage(IPackage package, params PackageOperation[] operations)
		{
			fakePackageOperationResolver
				.Stub(resolver => resolver.ResolveOperations(package))
				.Return(operations);
		}
		
		void CreateFakePackageResolverForUpdatePackageOperations()
		{
			fakePackageOperationResolver = MockRepository.GenerateStub<IPackageOperationResolver>();
			fakePackageOperationResolverFactory.UpdatePackageOperationsResolver = fakePackageOperationResolver;
		}
		
		[Test]
		public void ProjectManager_InstanceCreated_SourceRepositoryIsSharedRepositoryPassedToPackageManager()
		{
			CreatePackageManager();
			Assert.AreEqual(fakeSolutionSharedRepository, packageManager.ProjectManager.SourceRepository);
		}
		
		[Test]
		public void ProjectManager_InstanceCreated_LocalRepositoryIsPackageReferenceRepository()
		{
			CreatePackageManager();
			PackageReferenceRepository packageRefRepository = packageManager.ProjectManager.LocalRepository as PackageReferenceRepository;
			Assert.IsNotNull(packageRefRepository);
		}
		
		[Test]
		public void ProjectManager_InstanceCreated_LocalRepositoryIsRegisteredWithSharedRepository()
		{
			CreateTestProject();
			CreatePackageReferenceRepositoryHelper();
			
			string expectedPath = @"c:\projects\Test\MyProject";
			packageRefRepositoryHelper.FakeProjectSystem.PathToReturnFromGetFullPath = expectedPath;
			
			CreatePackageManager(testProject, packageRefRepositoryHelper);
			
			string actualPath = fakeSolutionSharedRepository.PathPassedToRegisterRepository;
			
			Assert.AreEqual(expectedPath, actualPath);
		}
		
		[Test]
		public void ProjectManager_InstanceCreated_PathResolverIsPackageManagerPathResolver()
		{
			CreatePackageManager();
			
			Assert.AreEqual(packageManager.PathResolver, packageManager.ProjectManager.PathResolver);
		}
		
		[Test]
		public void InstallPackage_PackageInstancePassed_AddsReferenceToProject()
		{
			CreatePackageManager();
			FakePackage package = InstallPackage();
			
			Assert.AreEqual(package, testableProjectManager.PackagePassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstancePassed_DependenciesNotIgnoredWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackage();
			
			Assert.IsFalse(testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstancePassed_PrereleaseVersionsNotAllowedWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackage();
			
			Assert.IsFalse(testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndPackageOperationsPassed_AddsReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = InstallPackageWithNoPackageOperations();
				
			Assert.AreEqual(package, testableProjectManager.PackagePassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndPackageOperationsPassed_DoNotIgnoreDependenciesWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageWithNoPackageOperations();
				
			Assert.IsFalse(testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndPackageOperationsPassed_DoNotAllowPrereleaseVersionsWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageWithNoPackageOperations();
				
			Assert.IsFalse(testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndPackageOperationsPassedAndIgnoreDependenciesIsTrue_IgnoreDependenciesWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageWithNoPackageOperationsAndIgnoreDependencies();
				
			Assert.IsTrue(testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndPackageOperationsPassedAndAllowPrereleaseVersionsIsTrue_PrereleaseVersionsallowedWhenAddingReferenceToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageWithNoPackageOperationsAndAllowPrereleaseVersions();
				
			Assert.IsTrue(testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageInstanceAndOneInstallPackageOperationPassed_PackageDefinedInOperationIsInstalledInLocalRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			PackageOperation operation = CreateOneInstallPackageOperation();
			InstallPackageWithPackageOperations(operation);
			
			Assert.AreEqual(operation.Package, fakeSolutionSharedRepository.FirstPackageAdded);
		}
		
		[Test]
		public void InstallPackage_PackageDependenciesIgnored_IgnoreDependenciesPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageAndIgnoreDependencies();
			
			Assert.IsTrue(testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_AllowPrereleaseVersions_AllowPrereleaseVersionsPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageAndAllowPrereleaseVersions();
			
			Assert.IsTrue(testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageDependenciesIgnored_AddsReferenceToPackage()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = InstallPackageAndIgnoreDependencies();
			
			Assert.AreEqual(package, testableProjectManager.PackagePassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageDependenciesNotIgnored_IgnoreDependenciesPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			InstallPackageAndDoNotIgnoreDependencies();
			
			Assert.IsFalse(testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}
		
		[Test]
		public void InstallPackage_PackageDependenciesNotIgnored_AddsReferenceToPackage()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = InstallPackageAndDoNotIgnoreDependencies();
			
			Assert.AreEqual(package, testableProjectManager.PackagePassedToAddPackageReference);
		}
		
		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_RemovesReferenceFromProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = UninstallPackage();
			
			Assert.AreEqual(package.Id, testableProjectManager.PackagePassedToRemovePackageReference.Id);
		}
		
		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_DoesNotRemoveReferenceForcefullyFromProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			UninstallPackage();
			
			Assert.IsFalse(testableProjectManager.ForcePassedToRemovePackageReference);
		}
		
		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_DependenciesNotRemovedWhenPackageReferenceRemovedFromProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			UninstallPackage();
			
			Assert.IsFalse(testableProjectManager.RemoveDependenciesPassedToRemovePackageReference);
		}
		
		[Test]
		public void UninstallPackage_PassingForceRemove_ReferenceForcefullyRemovedFromProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			UninstallPackageAndForceRemove();
			
			Assert.IsTrue(testableProjectManager.ForcePassedToRemovePackageReference);
		}
		
		[Test]
		public void UninstallPackage_PassingRemoveDependencies_DependenciesRemovedWhenPackageReferenceRemovedFromProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			UninstallPackageAndRemoveDependencies();
			
			Assert.IsTrue(testableProjectManager.RemoveDependenciesPassedToRemovePackageReference);
		}
		
		[Test]
		public void UninstallPackage_ProjectLocalRepositoryHasPackage_PackageRemovedFromProjectRepositoryBeforeSolutionRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			FakePackage package = CreateFakePackage();
			package.Id = "Test";
			
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			
			IPackage packageRemovedFromProject = null;
			packageManager.PackageUninstalled += (sender, e) => {
				packageRemovedFromProject = testableProjectManager.PackagePassedToRemovePackageReference;
			};
			packageManager.UninstallPackage(package);
			
			Assert.AreEqual("Test", packageRemovedFromProject.Id);
		}
		
		[Test]
		public void UninstallPackage_PackageReferencedByNoProjects_PackageIsRemovedFromSharedSolutionRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			FakePackage package = CreateFakePackage();
			package.Id = "MyPackageId";
			
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			fakeSolutionSharedRepository.FakePackages.Add(package);

			packageManager.UninstallPackage(package);
			
			bool containsPackage = fakeSolutionSharedRepository.FakePackages.Contains(package);
			
			Assert.IsFalse(containsPackage);
		}
		
		[Test]
		public void UninstallPackage_PackageReferencedByTwoProjects_PackageIsNotRemovedFromSharedSolutionRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			var package = new FakePackage("MyPackageId", "1.4.5.2");
			
			testableProjectManager.FakeLocalRepository.FakePackages.Add(package);
			fakeSolutionSharedRepository.FakePackages.Add(package);
			fakeSolutionSharedRepository.PackageIdsReferences.Add("MyPackageId");

			packageManager.UninstallPackage(package);
			
			bool containsPackage = fakeSolutionSharedRepository.FakePackages.Contains(package);
			
			Assert.IsTrue(containsPackage);
			Assert.AreEqual("MyPackageId", fakeSolutionSharedRepository.PackageIdPassedToIsReferenced);
			Assert.AreEqual(package.Version, fakeSolutionSharedRepository.VersionPassedToIsReferenced);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_PackageOperationsReturnedFromPackageOperationsResolverCreated()
		{
			CreatePackageManager();
			var package = new FakePackage();
			IEnumerable<PackageOperation> operations = GetInstallPackageOperations(package);
			
			IEnumerable<PackageOperation> expectedOperations = fakePackageOperationResolverFactory.FakeInstallPackageOperationResolver.PackageOperations;
			
			Assert.AreEqual(expectedOperations, operations);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesLocalRepositoryWhenGettingPackageOperations()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			IPackageRepository expectedRepository = packageManager.LocalRepository;
			IPackageRepository actualRepository = fakePackageOperationResolverFactory.LocalRepositoryPassedToCreateInstallPackageOperationsResolver;
			
			Assert.AreEqual(expectedRepository, actualRepository);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesSourceRepositoryWhenGettingPackageOperations()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			IPackageRepository expectedRepository = packageManager.SourceRepository;
			IPackageRepository actualRepository = fakePackageOperationResolverFactory.SourceRepositoryPassedToCreateInstallPackageOperationsResolver;
			
			Assert.AreEqual(expectedRepository, actualRepository);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_DependenciesNotIgnored()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			bool result = fakePackageOperationResolverFactory.IgnoreDependenciesPassedToCreateInstallPackageOperationResolver;
			
			Assert.IsFalse(result);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesLoggerWhenGettingPackageOperations()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			ILogger expectedLogger = packageManager.Logger;
			ILogger actualLogger = fakePackageOperationResolverFactory.LoggerPassedToCreateInstallPackageOperationResolver;
			
			Assert.AreEqual(expectedLogger, actualLogger);
		}
		
		[Test]
		public void GetInstallPackageOperations_PackageOperationResolverFactoryUsed_PackageUsedWhenGettingPackageOperations()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			IPackage actualPackage = fakePackageOperationResolverFactory
				.FakeInstallPackageOperationResolver
				.PackagePassedToResolveOperations;
			
			Assert.AreEqual(package, actualPackage);
		}
		
		[Test]
		public void GetInstallPackageOperations_IgnoreDependenciesIsTrue_PackageOperationResolverIgnoresDependencies()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperationsAndIgnoreDependencies(package);
			
			bool result = fakePackageOperationResolverFactory.IgnoreDependenciesPassedToCreateInstallPackageOperationResolver;
			
			Assert.IsTrue(result);
		}
		
		[Test]
		public void GetInstallPackageOperations_AllowPrereleaseVersionsIsTrue_PackageOperationResolverAllowsPrereleaseVersions()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperationsAndAllowPrereleaseVersions(package);
			
			bool result = fakePackageOperationResolverFactory.AllowPrereleaseVersionsPassedToCreateInstallPackageOperationResolver;
			
			Assert.IsTrue(result);
		}
		
		[Test]
		public void GetInstallPackageOperations_AllowPrereleaseVersionsIsFalse_PackageOperationResolverDoesNotAllowPrereleaseVersions()
		{
			CreatePackageManager();
			var package = new FakePackage();
			GetInstallPackageOperations(package);
			
			bool result = fakePackageOperationResolverFactory.AllowPrereleaseVersionsPassedToCreateInstallPackageOperationResolver;
			
			Assert.IsFalse(result);
		}
		
		public void UpdatePackage_PackageInstanceAndNoPackageOperationsPassed_UpdatesReferenceInProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = UpdatePackageWithNoPackageOperations();
			
			Assert.AreEqual(package, testableProjectManager.PackagePassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackage_PackageInstanceAndNoPackageOperationsPassed_UpdatesDependenciesInProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = UpdatePackageWithNoPackageOperations();
			
			Assert.IsTrue(testableProjectManager.UpdateDependenciesPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackage_PackageInstanceAndAllowPrereleaseVersionsIsTrue_PrereleaseVersionsAllowedToUpdateProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = UpdatePackageWithNoPackageOperationsAndAllowPrereleaseVersions();
			
			Assert.IsTrue(testableProjectManager.AllowPrereleaseVersionsPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackage_PackageInstanceAndAllowPrereleaseVersionsIsFalse_PrereleaseVersionsNotAllowedToUpdateProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = UpdatePackageWithNoPackageOperations();
			
			Assert.IsFalse(testableProjectManager.AllowPrereleaseVersionsPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackage_PackageInstanceAndOneInstallPackageOperationPassed_PackageDefinedInOperationIsInstalledInLocalRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			
			PackageOperation operation = CreateOneInstallPackageOperation();
			UpdatePackageWithPackageOperations(operation);
			
			Assert.AreEqual(operation.Package, fakeSolutionSharedRepository.FirstPackageAdded);
		}
		
		[Test]
		public void UpdatePackage_UpdateDependenciesSetToFalse_DependenciesInProjectNotUpdated()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			UpdatePackageWithNoPackageOperationsAndDoNotUpdateDependencies();
			
			Assert.IsFalse(testableProjectManager.UpdateDependenciesPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_OnePackage_PackageReferencedIsAddedToProject()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(package);
			
			packageManager.UpdatePackages(action);
			
			Assert.AreEqual(package, testableProjectManager.PackagePassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_TwoPackages_PackageReferencedIsAddedToProjectForBothPackages()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package1 = CreateFakePackage("First", "1.1");
			FakePackage package2 = CreateFakePackage("Second", "2.0");
			var expectedPackages = new FakePackage[] { package1, package2 };
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(expectedPackages);
			
			packageManager.UpdatePackages(action);
			
			PackageCollectionAssert.AreEqual(expectedPackages, testableProjectManager.PackagesPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_UpdateDependenciesIsTrue_UpdateDependenciesPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(package);
			action.UpdateDependencies = true;
			
			packageManager.UpdatePackages(action);
			
			Assert.IsTrue(testableProjectManager.UpdateDependenciesPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_UpdateDependenciesIsFalse_UpdateDependenciesPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(package);
			action.UpdateDependencies = false;
			
			packageManager.UpdatePackages(action);
			
			Assert.IsFalse(testableProjectManager.UpdateDependenciesPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_AllowPrereleaseVersionsIsTrue_AllowPrereleaseVersionsPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(package);
			action.AllowPrereleaseVersions = true;
			
			packageManager.UpdatePackages(action);
			
			Assert.IsTrue(testableProjectManager.AllowPrereleaseVersionsPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_AllowPrereleaseVersionsIsFalse_AllowPrereleaseVersionsPassedToProjectManager()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithPackages(package);
			action.AllowPrereleaseVersions = false;
			
			packageManager.UpdatePackages(action);
			
			Assert.IsFalse(testableProjectManager.AllowPrereleaseVersionsPassedToUpdatePackageReference);
		}
		
		[Test]
		public void UpdatePackages_TwoPackageOperations_BothPackagesInOperationsAddedToSharedRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			PackageOperation operation1 = CreateOneInstallPackageOperation("First", "1.0");
			PackageOperation operation2 = CreateOneInstallPackageOperation("Second", "1.0");
			UpdatePackagesAction action = CreateUpdatePackagesActionWithOperations(operation1, operation2);
			var expectedPackages = new FakePackage[] {
				operation1.Package as FakePackage,
				operation2.Package as FakePackage
			};
			
			packageManager.UpdatePackages(action);
			
			PackageCollectionAssert.AreEqual(expectedPackages, fakeSolutionSharedRepository.PackagesAdded);
		}
		
		[Test]
		public void GetUpdatePackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesLoggerWhenGettingPackageOperations()
		{
			CreatePackageManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction updateAction = CreateUpdatePackagesActionWithPackages(package);
			
			packageManager.GetUpdatePackageOperations(updateAction.Packages, updateAction);
			
			ILogger expectedLogger = packageManager.Logger;
			ILogger actualLogger = fakePackageOperationResolverFactory.LoggerPassedToCreateUpdatePackageOperationResolver;
			Assert.AreEqual(expectedLogger, actualLogger);
		}
		
		[Test]
		public void GetUpdatePackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesLocalRepositoryWhenGettingPackageOperations()
		{
			CreatePackageManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction updateAction = CreateUpdatePackagesActionWithPackages(package);
			
			packageManager.GetUpdatePackageOperations(updateAction.Packages, updateAction);
			
			IPackageRepository expectedRepository = packageManager.LocalRepository;
			IPackageRepository actualRepository = fakePackageOperationResolverFactory.LocalRepositoryPassedToCreateUpdatePackageOperationsResolver;
			Assert.AreEqual(expectedRepository, actualRepository);
		}
		
		[Test]
		public void GetUpdatePackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesSourceRepositoryWhenGettingPackageOperations()
		{
			CreatePackageManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction updateAction = CreateUpdatePackagesActionWithPackages(package);
			
			packageManager.GetUpdatePackageOperations(updateAction.Packages, updateAction);
			
			IPackageRepository expectedRepository = packageManager.SourceRepository;
			IPackageRepository actualRepository = fakePackageOperationResolverFactory.SourceRepositoryPassedToCreateUpdatePackageOperationsResolver;
			Assert.AreEqual(expectedRepository, actualRepository);
		}
		
		[Test]
		public void GetUpdatePackageOperations_PackageOperationResolverFactoryUsed_PackageManagerUsesUpdatePackageSettingsWhenGettingPackageOperations()
		{
			CreatePackageManager();
			FakePackage package = CreateFakePackage("Test", "1.1");
			UpdatePackagesAction updateAction = CreateUpdatePackagesActionWithPackages(package);
			
			packageManager.GetUpdatePackageOperations(updateAction.Packages, updateAction);
			
			IUpdatePackageSettings settings = fakePackageOperationResolverFactory.SettingsPassedToCreatePackageOperationResolver;
			Assert.AreEqual(updateAction, settings);
		}
		
		[Test]
		public void GetUpdatePackageOperations_TwoPackages_ReturnsPackageOperationsForBothPackages()
		{
			CreatePackageManager();
			CreateFakePackageResolverForUpdatePackageOperations();
			IPackageFromRepository package1 = CreateFakePackage("Test", "1.0");
			IPackageFromRepository package2 = CreateFakePackage("Test2", "1.0");
			PackageOperation operation1 = AddInstallOperationForPackage(package1);
			PackageOperation operation2 = AddInstallOperationForPackage(package2);
			UpdatePackagesAction updateAction = CreateUpdatePackagesActionWithPackages(package1, package2);
			
			List<PackageOperation> operations = packageManager.GetUpdatePackageOperations(updateAction.Packages, updateAction).ToList();
			
			Assert.AreEqual(2, operations.Count());
			Assert.Contains(operation1, operations);
			Assert.Contains(operation2, operations);
		}
		
		[Test]
		public void RunPackageOperations_TwoPackageOperations_BothPackagesInOperationsAddedToSharedRepository()
		{
			CreatePackageManager();
			CreateTestableProjectManager();
			PackageOperation operation1 = CreateOneInstallPackageOperation("First", "1.0");
			PackageOperation operation2 = CreateOneInstallPackageOperation("Second", "1.0");
			var operations = new PackageOperation[] { operation1, operation2 };
			var expectedPackages = new FakePackage[] {
				operation1.Package as FakePackage,
				operation2.Package as FakePackage
			};
			
			packageManager.RunPackageOperations(operations);
			
			PackageCollectionAssert.AreEqual(expectedPackages, fakeSolutionSharedRepository.PackagesAdded);
		}
	}
}
