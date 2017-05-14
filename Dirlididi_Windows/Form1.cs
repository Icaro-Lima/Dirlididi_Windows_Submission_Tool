using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Dirlididi_Windows
{
    public partial class Form1 : Form
    {
        public const string dirlididiApiBaseAddress = "http://dirlididi.com/api/";
        public string javaJdkPath = "";

        public struct Test
        {
            public string date;
            public string description;
            public string key;
            public string input;
            public string owner;
            public string output;
            public string tip;
            public bool publish;
        }

        public struct Problem
        {
            public List<Test> tests;
            public string date;
            public string name;
            public List<string> tags;
            public string tip;
            public bool publish;
            public bool canceled;
            public string key;
            public string owner;
            public string description;
        }

        public struct Submit
        {
            public List<List<string>> tests;
            public string key;
            public string code;
            public string token;
        }

        public struct Result
        {
            public string date;
            public string code;
            public bool ok;
            public string result;
            public string key;
            public string owner;
        }

        public struct TaskResult
        {
            public string stdOut;
            public string stdErr;

            public TaskResult(string stdOut, string stdErr)
            {
                this.stdOut = stdOut;
                this.stdErr = stdErr;
            }
        }

        public Form1()
        {
            InitializeComponent();

            if (File.Exists("configs.txt"))
            {
                StreamReader streamReaderConfigs = new StreamReader("configs.txt");

                string line;
                while ((line = streamReaderConfigs.ReadLine()) != null)
                {
                    string[] lineSplit = line.Split('=');
                    if (lineSplit[0] == "textBoxToken.Text")
                    {
                        textBoxToken.Text = lineSplit[1];
                    }
                    else if (lineSplit[0] == "textBoxProblemID.Text")
                    {
                        textBoxProblemID.Text = lineSplit[1];
                    }
                    else if (lineSplit[0] == "labelFilePath.Text")
                    {
                        labelFilePath.Text = lineSplit[1];
                    }
                    else if (lineSplit[0] == "javaJdkPath")
                    {
                        javaJdkPath = lineSplit[1];
                    }
                }
                streamReaderConfigs.Close();
            }

            if (!Directory.Exists(javaJdkPath))
            {
                bool find = false;

                if (Directory.Exists(@"C:\Program Files\Java"))
                {
                    string[] directories = Directory.GetDirectories(@"C:\Program Files\Java", "*", SearchOption.TopDirectoryOnly);
                    string directory = "";
                    for (int i = 0; i < directories.Length; i++)
                    {
                        if (Path.GetFileName(directories[i]).StartsWith("jdk"))
                        {
                            directory = directories[i];
                            break;
                        }
                    }

                    if (directory != "")
                    {
                        javaJdkPath = directory + "\\bin";
                        find = true;
                    }
                }
                else if (Directory.Exists(@"C:\Program Files (x86)\Java"))
                {
                    string[] directories = Directory.GetDirectories(@"C:\Program Files\Java", "*", SearchOption.TopDirectoryOnly);
                    string directory = "";
                    for (int i = 0; i < directories.Length; i++)
                    {
                        if (Path.GetFileName(directories[i]).StartsWith("jdk"))
                        {
                            directory = directories[i];
                            break;
                        }
                    }

                    if (directory != "")
                    {
                        javaJdkPath = directory + "\\bin";
                        find = true;
                    }
                }

                if (find)
                {
                    MessageBox.Show("Provavelmente consegui encotrar o diretório jdk do seu java (" + javaJdkPath + ") caso aconteça algum erro, mude manualmente no diretório atual do programa após fechar ele.");
                }
                else
                {
                    while (true)
                    {
                        MessageBox.Show("Não consegui encontrar o diretório jdk do seu java, na tela a seguir selecione ele. Ex: C:\\Program Files\\Java\\jdk1.8.0_131");

                        FolderBrowserDialog folderBrowserDialogJavaJdk = new FolderBrowserDialog();
                        if (folderBrowserDialogJavaJdk.ShowDialog() == DialogResult.OK)
                        {
                            folderBrowserDialogJavaJdk.SelectedPath += "\\bin";

                            if (!Directory.Exists(folderBrowserDialogJavaJdk.SelectedPath))
                            {
                                continue;
                            }

                            string[] files = Directory.GetFiles(folderBrowserDialogJavaJdk.SelectedPath, "*", SearchOption.TopDirectoryOnly);

                            bool findJava = false;
                            bool findJavac = false;
                            for (int i = 0; i < files.Length; i++)
                            {
                                files[i] = Path.GetFileName(files[i]);
                                if (files[i] == "java.exe")
                                {
                                    findJava = true;
                                }
                                else if (files[i] == "javac.exe")
                                {
                                    findJavac = true;
                                }
                            }

                            if (findJava && findJavac)
                            {
                                javaJdkPath = folderBrowserDialogJavaJdk.SelectedPath;
                                MessageBox.Show("Provavelmente consegui encotrar o diretório jdk do seu java (" + javaJdkPath + ") caso aconteça algum erro, mude manualmente no diretório atual do programa após fechar ele.");
                                break;
                            }
                            else if (findJava && !findJavac)
                            {
                                MessageBox.Show("Você selecionou o diretório do jre, estamos procurando pelo jdk. ;)");
                                continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Hmm... Ok! Se mudar de ideia pode abrir e fechar o programa ou modificar manualmente (após fechar) no diretório atual dele.");
                            break;
                        }
                    }
                }
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Arquivo .java|*.java";
            openFileDialog.FileName = labelFilePath.Text;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                labelFilePath.Text = openFileDialog.FileName;
            }
            openFileDialog.Dispose();
        }

        private void buttonSubmit_Click(object sender, EventArgs e)
        {
            buttonSubmit.Enabled = false;

            richTextBoxResult.Clear();

            WebClient webclient = new WebClient();
            webclient.Encoding = Encoding.UTF8;

            richTextBoxResult.AppendText("Baixando casos de teste da questão...\n");

            webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webclient_DownloadStringCompleted);
            webclient.DownloadStringAsync(new Uri(dirlididiApiBaseAddress + "problem/" + textBoxProblemID.Text));
        }

        private void webclient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    Problem problem = Newtonsoft.Json.JsonConvert.DeserializeObject<Problem>(e.Result);

                    richTextBoxResult.AppendText("Compilando...\n");

                    ProcessStartInfo processStartInfoCompile = new ProcessStartInfo(javaJdkPath + "\\javac.exe", "\"" + labelFilePath.Text + '"');
                    processStartInfoCompile.WindowStyle = ProcessWindowStyle.Hidden;
                    processStartInfoCompile.CreateNoWindow = true;
                    processStartInfoCompile.UseShellExecute = false;
                    processStartInfoCompile.RedirectStandardError = true;

                    Process processCompile = Process.Start(processStartInfoCompile);
                    processCompile.WaitForExit();

                    string error = processCompile.StandardError.ReadToEnd();
                    if (error == "")
                    {
                        richTextBoxResult.AppendText("Sucesso na compilação!\n\n", Color.Green);

                        List<List<string>> listOfResultsAndKeys = new List<List<string>>();

                        bool haveError = false;

                        for (int i = 0; i < problem.tests.Count; i++)
                        {
                            richTextBoxResult.AppendText(string.Format("Teste #{0}: \n", i));

                            if (problem.tests[i].publish)
                            {
                                ProcessStartInfo processStartInfoExecute = new ProcessStartInfo();
                                processStartInfoExecute.WorkingDirectory = labelFilePath.Text.Remove(labelFilePath.Text.LastIndexOf('\\'));
                                processStartInfoExecute.FileName = "java";
                                string fileName = Path.GetFileName(labelFilePath.Text);
                                processStartInfoExecute.Arguments = fileName.Remove(fileName.LastIndexOf('.'));
                                processStartInfoExecute.WindowStyle = ProcessWindowStyle.Hidden;
                                processStartInfoExecute.CreateNoWindow = true;
                                processStartInfoExecute.UseShellExecute = false;
                                processStartInfoExecute.RedirectStandardInput = true;
                                processStartInfoExecute.RedirectStandardOutput = true;
                                processStartInfoExecute.RedirectStandardError = true;

                                Process processExecute = Process.Start(processStartInfoExecute);
                                processExecute.StandardInput.WriteLine(problem.tests[i].input);
                                bool tle = processExecute.WaitForExit(2000);

                                error = processExecute.StandardError.ReadToEnd();
                                string output = processExecute.StandardOutput.ReadToEnd();

                                if (!tle)
                                {
                                    richTextBoxResult.AppendText("TLE (2000)!\n\n", Color.Blue);
                                }
                                else if (error != "")
                                {
                                    richTextBoxResult.AppendText(error + "\n\n", Color.Red);
                                    haveError = true;
                                }
                                else if (output.Replace("\r\n", "\n") != problem.tests[i].output + "\n")
                                {
                                    List<string> miniList = new List<string>(2);
                                    miniList.AddRange(new string[] { problem.tests[i].key, output.Replace("\r\n", "\n") });
                                    listOfResultsAndKeys.Add(miniList);

                                    richTextBoxResult.AppendText("Diferenças na saída...\n", Color.Red);

                                    StringReader stringReader0 = new StringReader(output);
                                    StringReader stringReader1 = new StringReader(problem.tests[i].output);

                                    string line0 = "", line1 = "";
                                    while (((line0 = stringReader0.ReadLine()) != null) | ((line1 = stringReader1.ReadLine()) != null))
                                    {
                                        if (line0 != null)
                                        {
                                            richTextBoxResult.AppendText("(saída)    > " + line0 + "\n");
                                        }
                                        if (line1 != null)
                                        {
                                            richTextBoxResult.AppendText("(esperado) > " + line1 + "\n");
                                        }
                                        richTextBoxResult.AppendText("\n");
                                    }
                                    richTextBoxResult.AppendText("\n");
                                }
                                else
                                {
                                    List<string> miniList = new List<string>(2);
                                    miniList.AddRange(new string[] { problem.tests[i].key, output.Replace("\r\n", "\n") });
                                    listOfResultsAndKeys.Add(miniList);

                                    richTextBoxResult.AppendText("OK!\n\n", Color.Green);
                                }
                            }
                            else
                            {
                                richTextBoxResult.AppendText("Teste não publicado. Ignore.\n\n");
                            }
                        }

                        if (!haveError)
                        {
                            richTextBoxResult.AppendText("Fim dos testes locais.\n\nComeçando a enviar pro servidor e ver o que ele retorna...\n\n");

                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(dirlididiApiBaseAddress + "code/" + problem.key);
                            request.Method = "POST";

                            Submit submit = new Submit();
                            submit.tests = listOfResultsAndKeys;
                            submit.key = problem.key;
                            submit.token = textBoxToken.Text;
                            submit.code = File.ReadAllText(labelFilePath.Text);

                            byte[] data = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(submit));
                            request.ContentLength = data.Length;

                            using (var stream = request.GetRequestStream())
                            {
                                stream.Write(data, 0, data.Length);
                            }

                            try
                            {
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                                Result result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(responseString);

                                bool haveErrorOnServer = false;
                                for (int i = 0; i < result.result.Length; i++)
                                {
                                    if (result.result[i] != '.')
                                    {
                                        haveError = true;
                                    }
                                }

                                if (!haveErrorOnServer)
                                {
                                    richTextBoxResult.AppendText("Julgamento do servidor: " + result.result + '\n', Color.Green);
                                }
                                else
                                {
                                    richTextBoxResult.AppendText("Julgamento do servidor: " + result.result + '\n', Color.Red);
                                }

                                buttonSubmit.Enabled = true;
                            }
                            catch (Exception ex)
                            {
                                richTextBoxResult.AppendText("O servidor retornou algum erro:\n" + ex.Message + '\n');

                                buttonSubmit.Enabled = true;
                            }
                        }
                        else
                        {
                            richTextBoxResult.AppendText("Como houve erro, não será enviado ao servidor.\n\n");

                            buttonSubmit.Enabled = true;
                        }
                    }
                    else
                    {
                        richTextBoxResult.AppendText("Erro na hora de compilar...\n" + error + '\n');

                        buttonSubmit.Enabled = true;
                    }
                }
                else
                {
                    richTextBoxResult.AppendText(e.Error.Message + '\n');
                    buttonSubmit.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                richTextBoxResult.AppendText("Houve algum erro durante toda a operação: " + ex.Message + '\n');

                buttonSubmit.Enabled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StreamWriter streamWriterConfigs = new StreamWriter("configs.txt", false);
            streamWriterConfigs.WriteLine("textBoxToken.Text=" + textBoxToken.Text);
            streamWriterConfigs.WriteLine("textBoxProblemID.Text=" + textBoxProblemID.Text);
            streamWriterConfigs.WriteLine("labelFilePath.Text=" + labelFilePath.Text);
            streamWriterConfigs.WriteLine("javaJdkPath=" + javaJdkPath);
            streamWriterConfigs.Close();
        }
    }
}
