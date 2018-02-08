using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocumentDeleter
{
    public partial class Form1 : Form
    {
        #region DocumentDeleter MemberFields

        const string AccountEndpointKey = "AccountEndpoint";
        const string AccountKeyKey = "AccountKey";
        string ConnectionString { get; set; }

        string DatabaseId { get; set; }

        string CollectionId { get; set; }

        string PartitionKey { get; set; }

        string ConditionField { get; set; }

        string Condition { get; set; }

        string ConditionValue { get; set; }

        string Conjunction
        {
            get
            {
                return radioButton1.Checked ? "AND" : "OR";
            }
        }

        string QueryText { get; set; }

        Dictionary<string,string> JoinList { get; set; }
        
        static string EndpointUrl { get; set; }

        static string AuthorizationKey { get; set; }

        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpoint
        static DocumentClient DocumentClient { get; set; }

        #endregion

        #region Member Functions
        /// <summary>
        /// Tokenizes input and stores name value pairs.
        /// </summary>
        /// <param name="connectionString">The string to parse.</param>
        /// <returns>Tokenized collection.</returns>
        private static IDictionary<string, string> ParseStringIntoSettings(string connectionString)
        {
            IDictionary<string, string> settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] splitted = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string nameValue in splitted)
            {
                string[] splittedNameValue = nameValue.Split(new char[] { '=' }, 2);

                if (splittedNameValue.Length != 2)
                {
                    return null;
                }

                if (settings.ContainsKey(splittedNameValue[0]))
                {
                    return null;
                }

                settings.Add(splittedNameValue[0], splittedNameValue[1]);
            }

            return settings;
        }
        #endregion

        #region Constructors

        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region FormControls
        
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            ConnectionString = richTextBox1.Text;
        }

        private void richTextBox6_TextChanged(object sender, EventArgs e)
        {
            DatabaseId = richTextBox6.Text;
        }

        private void richTextBox7_TextChanged(object sender, EventArgs e)
        {
            CollectionId = richTextBox7.Text;
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            ConditionField = richTextBox2.Text;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Condition = comboBox1.SelectedItem.ToString();
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {
            ConditionValue = richTextBox3.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox5.Clear();
            richTextBox5.Hide();
            if (ConditionField == null || Condition == null || ConditionValue == null ||
                ConditionField == "" || Condition == "" || ConditionValue == "")
            {
                richTextBox5.Text = "Input Field, Condition & Value";
                richTextBox5.Show();
                return;
            }

            if (QueryText == null)
            {
                QueryText = "";
                JoinList = new Dictionary<string,string>();
            }
            else
            {
                QueryText += " " + Conjunction + " ";
            }
            if (ConditionField.Contains('/'))
            {
                if (!JoinList.ContainsKey(ConditionField.Split('/')[0]))
                {
                    JoinList.Add(ConditionField.Split('/')[0], "c");
                }
                string current = ConditionField;
                while (current.Contains('/'))
                {
                    string previous = current.Split('/')[0].Split('.').Last();
                    int index = current.IndexOf('/');
                    current = current.Substring(index + 1);
                    if (!JoinList.ContainsKey(current.Split('/')[0]))
                    {
                        JoinList.Add(current.Split('/')[0], previous);
                    }
                }
                KeyValuePair<string,string> temp = JoinList.Last();
                JoinList.Remove(JoinList.Last().Key);
                if (!JoinList.ContainsKey(temp.Key.Split('.').First()))
                {
                    JoinList.Add(temp.Key.Split('.').First(), temp.Value);
                }
                QueryText += current + " " + Condition + " '" + ConditionValue + "'";
                string joinText = "";
                foreach (var dict in JoinList)
                {
                    joinText += Environment.NewLine + "join " + dict.Key.Split('.').Last() + " in " + dict.Value.Split('.').Last() + "." + dict.Key;
                }
                richTextBox4.Text = "select c.id from c" + joinText +
                    Environment.NewLine + "where " + QueryText;
            }
            else
            {
                QueryText += "c." + ConditionField + Condition + " '" + ConditionValue + "'";
                richTextBox4.Text = "select c.id from c" + Environment.NewLine + "where " + QueryText;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            QueryText = null;
            richTextBox4.Text = QueryText;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox5.Clear();
            richTextBox5.Hide();
            if (ConnectionString == null || ConnectionString == "" || DatabaseId == null || CollectionId == null)
            {
                richTextBox5.Text = "Input ConnectionString, DatabaseId & CollectionId";
                richTextBox5.Show();
                return;
            }

            string queryText = "select c.id from c";
            if (richTextBox4.Text != null && richTextBox4.Text != "")
            {
                queryText = richTextBox4.Text;
            }
            queryText = queryText.Replace("\n"," ");

            IDictionary<string, string> settings = ParseStringIntoSettings(ConnectionString);
            EndpointUrl = settings[AccountEndpointKey];
            AuthorizationKey = settings[AccountKeyKey];
            DocumentClient = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey);

            SqlQuerySpec spec = new SqlQuerySpec();
            List<dynamic> idsToDelete;
            try
            {
                var query = DocumentClient.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                    queryText,
                    new FeedOptions { MaxItemCount = 100, PartitionKey = new PartitionKey(PartitionKey) });
                idsToDelete = query.ToList();

                foreach (Dictionary<string,object> item in idsToDelete)
                {
                    DocumentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item["id"].ToString()), new RequestOptions { PartitionKey = new PartitionKey(PartitionKey) }).Wait();
                }
                
            }
            catch (Exception ex)
            {
                richTextBox5.Text = "Exception caught! " + ex.Message;
                richTextBox5.Show();
                return;
            }

            if (idsToDelete.Count == 0)
            {
                richTextBox5.Text = "No Documents to Delete";
            }
            else
            {
                richTextBox5.Text = idsToDelete.Count + " documents deleted";
            }
            richTextBox5.Show();
        }

        private void richTextBox8_TextChanged(object sender, EventArgs e)
        {
            PartitionKey = richTextBox8.Text;
        }

        #endregion
    }
}
