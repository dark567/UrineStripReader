using System.Collections.Generic;

namespace ConsoleAppServer
{
    public class Model
    {
        public string Code { get; set; }

        public string Goods { get; set; }
        public string VidUnits { get; set; }

        public string Value01 { get; set; }

        public string Query { get; set; }

        public static List<Model> _modelModel;

        public Model()
        {
            _modelModel = new List<Model>();
        }
        public Model(string code, string goods, string value01, string vidUnits = "", string query = "")
        {
            this.Code = code;
            this.Goods = goods;
            this.VidUnits = vidUnits;
            this.Value01 = value01;
            this.Query = query;
        }
    }
}
