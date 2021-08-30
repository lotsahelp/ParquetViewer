﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Utilities;

namespace ParquetFileViewer
{
    public partial class MetadataViewer : Form
    {
        private static readonly string THRIFT_METADATA = "Thrift Metadata";
        private static readonly string APACHE_ARROW_SCHEMA = "ARROW:schema";
        private static readonly string PANDAS_SCHEMA = "pandas";
        private Parquet.ParquetReader parquetReader;

        public MetadataViewer(Parquet.ParquetReader parquetReader) : this()
        {
            this.parquetReader = parquetReader;
        }

        public MetadataViewer()
        {
            InitializeComponent();
        }

        private void MetadataViewer_Load(object sender, EventArgs e)
        {
            this.mainBackgroundWorker.RunWorkerAsync();
        }

        private void AddTab(string tabName, string text)
        {
            TabPage tab = new TabPage(tabName);
            tab.Controls.Add(new TextBox()
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = Color.LightGray,
                Text = text,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            });

            this.tabControl.TabPages.Add(tab);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainBackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var metadataResult = new List<(string TabName, string Text)>();
            if (parquetReader.ThriftMetadata != null)
            {
                string json = ParquetMetadataAnalyzers.ThriftMetadataToJSON(parquetReader.ThriftMetadata);
                metadataResult.Add((THRIFT_METADATA, json));
            }
            else
                metadataResult.Add((THRIFT_METADATA, "No thrift metadata available"));

            if (this.parquetReader.CustomMetadata != null)
            {
                foreach (var _customMetadata in this.parquetReader.CustomMetadata)
                {
                    string value = _customMetadata.Value;
                    if (PANDAS_SCHEMA.Equals(_customMetadata.Key))
                    {
                        value = ParquetMetadataAnalyzers.PandasSchemaToJSON(value);
                    }
                    else if (APACHE_ARROW_SCHEMA.Equals(_customMetadata.Key))
                    {
                        value = ParquetMetadataAnalyzers.ApacheArrowToJSON(value);
                    }

                    metadataResult.Add((_customMetadata.Key, value));
                }
            }

            e.Result = metadataResult;
        }

        private void MainBackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Something went wrong while reading the file's metadata: " +
                    $"{Environment.NewLine}{e.Error.ToString()}", "Metadata Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                this.tabControl.TabPages.Clear();
                if (e.Result is List<(string TabName, string Text)> tabs)
                {
                    foreach (var tab in tabs)
                    {
                        this.AddTab(tab.TabName, tab.Text);
                    }
                }
            }
        }
    }
}
