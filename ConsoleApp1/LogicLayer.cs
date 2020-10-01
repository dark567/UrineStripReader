namespace ConsoleAppServer
{
    public static class LogicLayer
    {

        /// <summary>
        /// 
        /// </summary>
        public static Model[] GetModel
        {
            get
            {
                return Model._modelModel.ToArray();
            }
        }

        /// <summary>
        /// Add Model
        /// </summary>
        /// <param name="_modelModel"></param>
        public static void AddModel(Model _modelModel)
        {
            Model._modelModel.Add(_modelModel);
        }

        /// <summary>
        /// Remove Model с указанным индексом
        /// </summary>
        /// <param name="Index">index list who removing</param>
        public static void RemoveModel(int Index)
        {
            Model._modelModel.RemoveAt(Index);
        }

        /// <summary>
        /// Remove Model с указанным индексом
        /// </summary>
        /// <param name="Index">index list who removing</param>
        public static void RemoveModelAll()
        {
            Model._modelModel.Clear();
        }
    }
}
