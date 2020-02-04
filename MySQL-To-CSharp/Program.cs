using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Fclp;

namespace MySQL_To_CSharp
{
    public class ApplicationArguments
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public string Table { get; set; }
        public bool GenerateConstructorAndOutput { get; set; }
        public bool GenerateMarkupPages { get; set; }
        public string MarkupDatabaseNameReplacement { get; set; }
        public string Namespace { get; set; }
    }

    public class Column
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public string ColumnType { get; set; }

        public Column(MySqlDataReader reader)
        {
            this.Name = reader.GetString(1);
            this.ColumnType = reader.GetString(2);
        }

        public override string ToString()
        {
            return $"public {this.Type.Name} {this.Name.FirstCharUpper()} {{ get; set; }}";
        }
    }

    public static class StringExtension
    {
        public static string FirstCharUpper(this string str)
        {
            return str.First().ToString().ToUpper() + str.Substring(1);
        }

        public static string CSharpClassNamingConvention(this string str)
        {
            string[] subs = str.Split('_');
            str = "";
            string first = subs.First();
            foreach (string substring in subs)
            {

                if (substring.Equals(first))
                {
                    str = substring.FirstCharUpper();
                }
                else
                {
                    str = str + "_" + substring.FirstCharUpper();
                }

            }
            return str;
        }
    }

    class Program
    {
        private static void DbToClasses(string dbName, Dictionary<string, List<Column>> db, bool generateConstructorAndOutput, ApplicationArguments args)
        {
            int indentationLevel = 0;
            if (!Directory.Exists(dbName))
                Directory.CreateDirectory(dbName);

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, List<Column>> table in db)
            {
                Console.WriteLine($"Creating file {table.Key.CSharpClassNamingConvention()} ...");

                // Using statements
                sb.AppendLine("using System;");
                if (args.GenerateConstructorAndOutput)
                    sb.AppendLine("using MySql.Data.MySqlClient;");
                sb.AppendLine("");


                if (!string.IsNullOrEmpty(args.Namespace))
                {
                    sb.AppendLine($"namespace {args.Namespace}");
                    sb.AppendLine("{");
                    indentationLevel++;
                }

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t"); // Adding indentation
                sb.AppendLine($"public class {table.Key.CSharpClassNamingConvention()}");

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("{");

                indentationLevel++;

                // properties
                foreach (Column column in table.Value)
                {
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine(column.ToString());
                }

                // Empty constructor for EF Core
                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("");

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine($"public {table.Key.CSharpClassNamingConvention()}()");

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("{");

                indentationLevel++;

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("");

                indentationLevel--;

                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("}");

                if (generateConstructorAndOutput)
                {
                    // SQL constructor
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"public {table.Key.CSharpClassNamingConvention()}(MySqlDataReader reader)");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("{");

                    indentationLevel++;

                    foreach (Column column in table.Value)
                    {
                        for (int i = 0; i < indentationLevel; i++) sb.Append("\t");

                        // check which type and use correct get method instead of casting
                        if (column.Type != typeof(string))
                            sb.AppendLine($"{column.Name.FirstCharUpper()} = Convert.To{column.Type.Name}(reader[\"{column.Name}\"].ToString());");
                        else
                            sb.AppendLine($"{column.Name.FirstCharUpper()} = reader[\"{column.Name}\"].ToString();");
                    }

                    indentationLevel--;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"}}");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("");


                    // update query
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"public string UpdateQuery()");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("{");

                    indentationLevel++;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.Append($"return $\"UPDATE `{table.Key}` SET");

                    foreach (Column column in table.Value)
                        sb.Append($" {column.Name} = {{{column.Name.FirstCharUpper()}}},");
                    sb.Remove(sb.ToString().LastIndexOf(','), 1);
                    sb.AppendLine($" WHERE {table.Value[0].Name} = {{{table.Value[0].Name.FirstCharUpper()}}};\";");

                    indentationLevel--;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"}}");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("");

                    // insert query
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"public string InsertQuery()");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("{");

                    indentationLevel++;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.Append($"return $\"INSERT INTO `{table.Key}` VALUES (");
                    foreach (Column column in table.Value)
                        sb.Append($" {{{column.Name.FirstCharUpper()}}},");
                    sb.Remove(sb.ToString().LastIndexOf(','), 1);
                    sb.AppendLine($");\";");

                    indentationLevel--;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"}}");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("");

                    // delete query
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"public string DeleteQuery()");

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("{");

                    indentationLevel++;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine($"return $\"DELETE FROM `{table.Key}` WHERE {table.Value[0].Name} = {{{table.Value[0].Name.FirstCharUpper()}}};\";");

                    indentationLevel--;

                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("}");
                }

                indentationLevel--;

                // class closing
                for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                sb.AppendLine("}");

                indentationLevel--;

                // Namespace closing
                if (!string.IsNullOrEmpty(args.Namespace))
                {
                    for (int i = 0; i < indentationLevel; i++) sb.Append("\t");
                    sb.AppendLine("}");
                }

                StreamWriter sw = new StreamWriter($"{dbName}/{table.Key.CSharpClassNamingConvention()}.cs", false);
                sw.Write(sb.ToString());
                sw.Close();
                sb.Clear();
            }
        }

        private static void DbToMarkupPage(string dbName, Dictionary<string, List<Column>> db)
        {
            string wikiDir = $"wiki";
            string wikiDbDir = $"{wikiDir}/{dbName}";
            string wikiTableDir = $"{wikiDbDir}/tables";

            if (!Directory.Exists(wikiDir))
                Directory.CreateDirectory(wikiDir);
            if (!Directory.Exists(wikiTableDir))
                Directory.CreateDirectory(wikiTableDir);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"* [[{dbName}|{dbName}]]");

            StreamWriter sw = new StreamWriter($"{wikiDir}/index.txt", true);
            sw.Write(sb.ToString());
            sw.Close();
            sb.Clear();

            sb.AppendLine($"[[Database Structure|Database Structure]] > [[{dbName}|{dbName}]]");

            // generate index pages
            foreach (KeyValuePair<string, List<Column>> table in db)
                sb.AppendLine($"* [[{table.Key.FirstCharUpper()}|{table.Key.ToLower()}]]");

            sw = new StreamWriter($"{wikiDbDir}/{dbName}.txt");
            sw.Write(sb.ToString());
            sw.Close();
            sb.Clear();

            foreach (KeyValuePair<string, List<Column>> table in db)
            {
                sb.AppendLine($"[[Database Structure|Database Structure]] > [[{dbName}|{dbName}]] > [[{table.Key}|{table.Key}]]");
                sb.AppendLine("");
                sb.AppendLine("Column | Type | Description");
                sb.AppendLine("--- | --- | ---");

                foreach (Column column in table.Value)
                    sb.AppendLine($"{column.Name.FirstCharUpper()} | {column.ColumnType} | ");
                sw = new StreamWriter($"{wikiTableDir}/{table.Key}.txt");
                sw.Write(sb.ToString());
                sw.Close();
                sb.Clear();
            }

        }

        static void Main(string[] args)
        {
            FluentCommandLineParser<ApplicationArguments> parser = new FluentCommandLineParser<ApplicationArguments>();
            parser.Setup(arg => arg.IP).As('i', "ip").SetDefault("127.0.0.1").WithDescription("(optional) IP address of the MySQL server, will use 127.0.0.1 if not specified");
            parser.Setup(arg => arg.Port).As('n', "port").SetDefault(3306).WithDescription("(optional) Port number of the MySQL server, will use 3306 if not specified");
            parser.Setup(arg => arg.User).As('u', "user").SetDefault("root").WithDescription("(optional) Username, will use root if not specified");
            parser.Setup(arg => arg.Password).As('p', "password").SetDefault(String.Empty).WithDescription("(optional) Password, will use empty password if not specified");
            
            parser.Setup(arg => arg.Database).As('d', "database").Required().WithDescription("Database name");
            
            parser.Setup(arg => arg.Table).As('t', "table").SetDefault(String.Empty).WithDescription("(optional) Table name, will generate entire database if not specified");
            parser.Setup(arg => arg.Namespace).As('s', "namespace").SetDefault(String.Empty).WithDescription("(optional) Namespace name, will add a namespace to the cs file.");
            parser.Setup(arg => arg.GenerateConstructorAndOutput).As('g', "generateconstructorandoutput").SetDefault(false).WithDescription("(optional) Generate a reading constructor and SQL statement output - Activate with -g true");
            parser.Setup(arg => arg.GenerateMarkupPages).As('m', "generatemarkuppages").SetDefault(false).WithDescription("(optional) Generate markup pages for database and tables which can be used in wikis - Activate with -m true");
            parser.Setup(arg => arg.MarkupDatabaseNameReplacement).As('r', "markupdatabasenamereplacement").SetDefault("").WithDescription("(optional) Will use this instead of database name for wiki breadcrump generation");
            parser.SetupHelp("?", "help").Callback(text => Console.WriteLine(text));

            ICommandLineParserResult result = parser.Parse(args);
           
            if (!result.HasErrors)
            {
                ApplicationArguments conf = parser.Object as ApplicationArguments;
                if (conf.Database is null)
                {
                    Console.WriteLine("You didn't specify a database");
                    return;
                }

                string confString =
                    $"Server={conf.IP};Port={conf.Port};Uid={conf.User};Pwd={conf.Password};Database={conf.Database}";
                Console.WriteLine("Database connection: {0}", confString);
                Console.WriteLine("Defined Namespace: {0}", conf.Namespace);

                Dictionary<string, List<Column>> database = new Dictionary<string, List<Column>>();

                using (MySqlConnection con = new MySqlConnection(confString))
                {
                    con.Open();
                    Console.WriteLine("Connection opened ...");

                    using (MySqlCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText =
                            $"SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{conf.Database}'";
                        if (!conf.Table.Equals(string.Empty))
                            cmd.CommandText += $" AND TABLE_NAME = '{conf.Table}'";

                        MySqlDataReader reader = cmd.ExecuteReader();
                        if (!reader.HasRows)
                            return;

                        while (reader.Read())
                            if (database.ContainsKey(reader.GetString(0)))
                                database[reader.GetString(0)].Add(new Column(reader));
                            else
                                database.Add(reader.GetString(0), new List<Column>() { new Column(reader) });
                    }

                    Console.WriteLine("Retrived table information ...");

                    foreach (KeyValuePair<string, List<Column>> table in database)
                    {
                        using (MySqlCommand cmd = con.CreateCommand())
                        {
                            // lul - is there a way to do this without this senseless statement?
                            cmd.CommandText = $"SELECT * FROM `{table.Key}` LIMIT 0";
                            MySqlDataReader reader = cmd.ExecuteReader();
                            DataTable schema = reader.GetSchemaTable();
                            foreach (Column column in table.Value)
                                column.Type = schema.Select($"ColumnName = '{column.Name}'")[0]["DataType"] as Type;
                        }
                    }

                    Console.WriteLine("Retrived column types ...");

                    con.Close();
                }

                DbToClasses(conf.Database, database, conf.GenerateConstructorAndOutput, conf);
                if (conf.GenerateMarkupPages)
                    DbToMarkupPage(String.IsNullOrEmpty(conf.MarkupDatabaseNameReplacement) ? conf.Database : conf.MarkupDatabaseNameReplacement, database);
                Console.WriteLine("Successfully generated C# classes!");
            }
            else
                Console.WriteLine("Entered command line arguments has errors!");

            Console.ReadLine();
        }
    }
}
