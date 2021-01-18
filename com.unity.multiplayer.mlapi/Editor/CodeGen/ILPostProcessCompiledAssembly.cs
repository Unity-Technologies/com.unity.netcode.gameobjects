using System.IO;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen
{
    internal class ILPostProcessCompiledAssembly : ICompiledAssembly
    {
        readonly string m_AssemblyFilename;
        readonly string m_OutputPath;
        InMemoryAssembly m_InMemoryAssembly;

        public ILPostProcessCompiledAssembly(string assName, string[] refs, string[] defines, string outputPath)
        {
            m_AssemblyFilename = assName;
            Name = Path.GetFileNameWithoutExtension(m_AssemblyFilename);
            References = refs;
            Defines = defines;

            m_OutputPath = outputPath;
        }

        public InMemoryAssembly InMemoryAssembly
        {
            get { return CreateOrGetInMemoryAssembly(); }
            set { m_InMemoryAssembly = value; }
        }

        public string Name { get; set; }
        public string[] References { get; set; }
        public string[] Defines { get; private set; }

        private InMemoryAssembly CreateOrGetInMemoryAssembly()
        {
            if (m_InMemoryAssembly != null)
            {
                return m_InMemoryAssembly;
            }

            byte[] peData = File.ReadAllBytes(Path.Combine(m_OutputPath, m_AssemblyFilename));

            var pdbFileName = Path.GetFileNameWithoutExtension(m_AssemblyFilename) + ".pdb";
            byte[] pdbData = File.ReadAllBytes(Path.Combine(m_OutputPath, pdbFileName));

            m_InMemoryAssembly = new InMemoryAssembly(peData, pdbData);
            return m_InMemoryAssembly;
        }
    }
}
