// Copyright (c) 2012-2020 Wojciech Figat. All rights reserved.

using System.IO;
using Flax.Build;

namespace Flax.Deps.Dependencies
{
    /// <summary>
    /// Khronos reference front-end for GLSL and ESSL, and sample SPIR-V generator
    /// </summary>
    /// <seealso cref="Flax.Deps.Dependency" />
    class glslang : Dependency
    {
        /// <inheritdoc />
        public override TargetPlatform[] Platforms
        {
            get
            {
                switch (BuildPlatform)
                {
                case TargetPlatform.Windows:
                    return new[]
                    {
                        TargetPlatform.Windows,
                    };
                default: return new TargetPlatform[0];
                }
            }
        }

        /// <inheritdoc />
        public override void Build(BuildOptions options)
        {
            var root = options.IntermediateFolder;
            var installDir = Path.Combine(root, "install");
            var buildDir = root;
            var solutionPath = Path.Combine(buildDir, "glslang.sln");
            var configuration = "Release";
            var cmakeArgs = string.Format("-DCMAKE_INSTALL_PREFIX=\"{0}\" -DENABLE_CTEST=OFF -DENABLE_HLSL=ON -DENABLE_SPVREMAPPER=ON -DENABLE_GLSLANG_BINARIES=OFF", installDir);
            var libsRoot = Path.Combine(installDir, "lib");
            var outputFiles = new[]
            {
                Path.Combine(libsRoot, "GenericCodeGen.lib"),
                Path.Combine(libsRoot, "MachineIndependent.lib"),
                Path.Combine(libsRoot, "HLSL.lib"),
                Path.Combine(libsRoot, "OSDependent.lib"),
                Path.Combine(libsRoot, "OGLCompiler.lib"),
                Path.Combine(libsRoot, "SPIRV-Tools-opt.lib"),
                Path.Combine(libsRoot, "SPIRV-Tools.lib"),
                Path.Combine(libsRoot, "SPIRV.lib"),
                Path.Combine(libsRoot, "glslang.lib"),
            };

            // Get the source
            CloneGitRepoFast(root, "https://github.com/FlaxEngine/glslang.git");

            // Setup the external sources
            Utilities.Run("python", "update_glslang_sources.py", null, root, Utilities.RunOptions.None);

            foreach (var platform in options.Platforms)
            {
                switch (platform)
                {
                case TargetPlatform.Windows:
                {
                    // Build for Win64
                    File.Delete(Path.Combine(buildDir, "CMakeCache.txt"));
                    RunCmake(buildDir, TargetPlatform.Windows, TargetArchitecture.x64, cmakeArgs);
                    Utilities.Run("cmake", string.Format("--build . --config {0} --target install", configuration), null, buildDir, Utilities.RunOptions.None);
                    Deploy.VCEnvironment.BuildSolution(solutionPath, configuration, "x64");
                    var depsFolder = GetThirdPartyFolder(options, TargetPlatform.Windows, TargetArchitecture.x64);
                    foreach (var file in outputFiles)
                    {
                        Utilities.FileCopy(file, Path.Combine(depsFolder, Path.GetFileName(file)));
                    }
                    break;
                }
                }
            }

            // Copy glslang headers
            foreach (var dir in new[]
            {
                "HLSL",
                "Include",
                "MachineIndependent",
                "Public",
                "SPIRV",
            })
            {
                var srcDir = Path.Combine(installDir, "include", "glslang", dir);
                var dstDir = Path.Combine(options.ThirdPartyFolder, "glslang", dir);
                Utilities.DirectoryDelete(dstDir);
                Utilities.DirectoryCopy(srcDir, dstDir);
            }

            // Copy spirv-tools headers
            foreach (var file in Directory.GetFiles(Path.Combine(options.ThirdPartyFolder, "spirv-tools"), "*.h"))
                File.Delete(file);
            foreach (var file in Directory.GetFiles(Path.Combine(options.ThirdPartyFolder, "spirv-tools"), "*.hpp"))
                File.Delete(file);
            foreach (var file in Directory.GetFiles(Path.Combine(installDir, "include", "spirv-tools"), "*.h"))
                Utilities.FileCopy(file, Path.Combine(options.ThirdPartyFolder, "spirv-tools", Path.GetFileName(file)));
            foreach (var file in Directory.GetFiles(Path.Combine(installDir, "include", "spirv-tools"), "*.hpp"))
                Utilities.FileCopy(file, Path.Combine(options.ThirdPartyFolder, "spirv-tools", Path.GetFileName(file)));
        }
    }
}