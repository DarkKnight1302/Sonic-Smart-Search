namespace SonicExplorer
{
    public abstract class ListItemContext
    {
        public string fileName;
        public string filePath;
        public bool isFileOrFolder;
        
        public virtual string FileName
        {
            get => this.fileName;
            set
            { 
                this.fileName = value;
            }
        }
        
        public virtual string FilePath
        {
            get => this.filePath;
            set
            { 
                this.filePath = value;
            }
        }
        
        public virtual bool IsFileOrFolder
        {
            get => this.IsFileOrFolder;
            set
            { 
                this.IsFileOrFolder = value;
            }
        }

    }
}
