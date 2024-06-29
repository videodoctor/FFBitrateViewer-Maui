using System;
using System.IO;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.Services
{
    public interface IFileEntry
    {
        Uri Path { get; }

        Task<Stream> OpenReadAsync();
    }
}