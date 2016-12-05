using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;


enum WordType : byte
{
    FullWord,
    PartialWord,
    FullWordAndPartialWord
}


namespace WordLookUp
{
    class Program
    {
        static Dictionary<string, WordType> _words = new Dictionary<string, WordType>(400000);
        static Dictionary<string, bool> _found = new Dictionary<string, bool>();
        const int _minLength = 2;

        static string Input() //Read cypher.xml file
        {
            //Read xml and put it on a string variable, splitting each node in different lines
            XmlTextReader reader = new XmlTextReader("cypher.xml");
            string result = "";

            while (reader.Read())
            {
                if ((reader.NodeType) == XmlNodeType.Text)
                    result += reader.Value + "\r\n";
            }
            return result.Remove(result.Length - 2);
        }

        static List<string[]> DefRules() //Read rules.xml file
        {
            //Read rules and setting them in a list with the source and replacement characters
            List<string[]> result = new List<string[]>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("rules.xml");

            string sourceValue = "";
            string replacementValue = "";

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/root/value");
            foreach (XmlNode node in nodeList)
            {
                sourceValue = node.SelectSingleNode("source").InnerText;
                replacementValue = node.SelectSingleNode("replacement").InnerText;

                result.Add(new string[] { sourceValue, replacementValue });
            }

            return result;
        }

        static List<List<ruleValue>> DefValues() //Read values.xml file
        {
            //Defined a new ruleValue object to store each value

            List<List<ruleValue>> result = new List<List<ruleValue>>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("values.xml");

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/root/value");
            foreach (XmlNode node in nodeList)
            {
                List<ruleValue> ruleList = new List<ruleValue>();
                bool isTerminationValue = new bool();
                int orderValue = new int();
                int ruleValue = new int();

                XmlNodeList nodeListNode = node.ChildNodes;
                foreach (XmlNode nodeNode in nodeListNode)
                {
                    isTerminationValue = bool.Parse(nodeNode.SelectSingleNode("isTermination").InnerText);
                    orderValue = int.Parse(nodeNode.SelectSingleNode("order").InnerText);
                    ruleValue = int.Parse(nodeNode.SelectSingleNode("rule").InnerText);

                    ruleList.Add(new ruleValue { isTermination = isTerminationValue, order = orderValue, rule = ruleValue });
                }
                result.Add(ruleList);
            }

            return result;
        }

        static string Markov(string origLine, List<string[]> rulesSet, List<ruleValue> valuesSet)
        {
            List<ruleValue> sortedValues = valuesSet.OrderBy(x => x.order).ToList();
            int cont = 0;

            while (cont < sortedValues.Count())
            {
                string[] currentRule = rulesSet[sortedValues[cont].rule];
                if (origLine.Contains(currentRule[0]))
                {
                    int index = origLine.IndexOf(currentRule[0]);
                    origLine = origLine.Substring(0, index) + currentRule[1] + origLine.Substring(index + currentRule[0].Length);
                    cont = 0;

                    if (sortedValues[cont].isTermination)
                        cont = sortedValues.Count();
                }
                else
                {
                    cont++;
                }
            }
            return origLine;
        }


        static void Main(string[] args)
        {
            //Program start
            //Read files cypher, rules, values
            Console.WriteLine("Starting program");
            Console.WriteLine("Files are located on /bin/Debug folder");
            Console.WriteLine("Reading cypher.xml file");
            string inputString = Input();
            Console.WriteLine("Reading rules.xml file");
            List<string[]> defRules = DefRules();
            Console.WriteLine("Reading values.xml file");
            List<List<ruleValue>> defValues = DefValues();

            //Process cypher file
            string[] lines = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            List<string> convertedLines = new List<string>();
            int contLine = 0;

            foreach (string line in lines)
            {
                string newLine = Markov(line, defRules, defValues[contLine]);
                contLine++;
                convertedLines.Add(newLine);
            }

            // Read words.xml and store words on dictionary
            using (XmlTextReader reader = new XmlTextReader("words.xml"))
            {
                while (reader.Read())
                {
                    if ((reader.NodeType) == XmlNodeType.Text)
                    {
                        string line = reader.Value;
                        if (line == null)
                        {
                            break;
                        }
                        if (line.Length >= _minLength)
                        {
                            for (int i = 1; i <= line.Length; i++)
                            {
                                string substring = line.Substring(0, i);
                                WordType value;
                                if (_words.TryGetValue(substring, out value))
                                {
                                    // If this is a full word.
                                    if (i == line.Length)
                                    {
                                        // If only partial word is stored.
                                        if (value == WordType.PartialWord)
                                        {
                                            // Upgrade type.
                                            _words[substring] = WordType.FullWordAndPartialWord;
                                        }
                                    }
                                    else
                                    {
                                        // If not a full word and only partial word is stored.
                                        if (value == WordType.FullWord)
                                        {
                                            _words[substring] = WordType.FullWordAndPartialWord;
                                        }
                                    }
                                }
                                else
                                {
                                    // If this is a full word.
                                    if (i == line.Length)
                                    {
                                        _words.Add(substring, WordType.FullWord);
                                    }
                                    else
                                    {
                                        _words.Add(substring, WordType.PartialWord);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Cypher.txt after markov rules");
            string[] lookupLines = convertedLines.ToArray();
            foreach (string line in lookupLines)
                Console.WriteLine(line);
            Console.WriteLine();

            // Put into 2D array.
            int height = lookupLines.Length;
            int width = lookupLines[0].Length;
            char[,] array = new char[height, width];
            for (int i = 0; i < width; i++)
            {
                for (int a = 0; a < height; a++)
                {
                    array[a, i] = lookupLines[a][i];
                }
            }

            // Create empty covered array.
            bool[,] covered = new bool[height, width];

            // Start at each square in the 2D array.
            for (int i = 0; i < width; i++)
            {
                for (int a = 0; a < height; a++)
                {
                    Search(array, i, a, width, height, "", covered);
                }
            }
            Console.ReadLine();
        }

        static void Search(char[,] array,
                            int i,
                            int a,
                            int width,
                            int height,
                            string build,
                            bool[,] covered)
        {
            // Check array bounds.
            if (i >= width ||
                i < 0 ||
                a >= height ||
                a < 0)
            {
                return;
            }
            // Check covered squares.
            if (covered[a, i])
            {
                return;
            }

            char letter = array[a, i];
            // Append.
            string pass = build + letter;
            // Check if full word.
            WordType value;
            if (_words.TryGetValue(pass, out value))
            {
                // Handle all full words.
                if (value == WordType.FullWord ||
                value == WordType.FullWordAndPartialWord)
                {
                    // Avoid duplicated words.
                    if (!_found.ContainsKey(pass))
                    {
                        Console.WriteLine("{0} found", pass);
                        _found.Add(pass, true);
                    }
                }
                // Handle all partial words.
                if (value == WordType.PartialWord ||
                value == WordType.FullWordAndPartialWord)
                {
                    // Copy covered array.
                    bool[,] cov = new bool[height, width];
                    for (int i2 = 0; i2 < width; i2++)
                    {
                        for (int a2 = 0; a2 < height; a2++)
                        {
                            cov[a2, i2] = covered[a2, i2];
                        }
                    }
                    // Set this current square as covered.
                    cov[a, i] = true;

                    // Recursive check in all directions
                    Search(array, i + 1, a, width, height, pass, cov);
                    Search(array, i, a + 1, width, height, pass, cov);
                    Search(array, i + 1, a + 1, width, height, pass, cov);
                    Search(array, i - 1, a, width, height, pass, cov);
                    Search(array, i, a - 1, width, height, pass, cov);
                    Search(array, i - 1, a - 1, width, height, pass, cov);
                    Search(array, i - 1, a + 1, width, height, pass, cov);
                    Search(array, i + 1, a - 1, width, height, pass, cov);
                }
            }
        }
    }
}
