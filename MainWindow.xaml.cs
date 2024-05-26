using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjetoERP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<Produto> _produtos = new ObservableCollection<Produto>();
        private decimal frete;
        private decimal _usdRate;
        private decimal _eurRate;
        private decimal _arsRate;
        private decimal _selectedRate = 1; // Default to BRL

        public ObservableCollection<Produto> Produtos
        {
            get { return _produtos; }
            set
            {
                _produtos = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Produtos)));
            }
        }

        public MainWindow()
        {
            Produtos = new ObservableCollection<Produto>(); // Inicialize a coleção de produtos
            InitializeComponent();
            DataContext = this; // Defina o DataContext como esta instância da janela
            CarregarDados();
        }

        private async void CarregarDados()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync("https://economia.awesomeapi.com.br/last/USD-BRL,EUR-BRL,ARS-BRL");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var rates = JObject.Parse(json);

                    _usdRate = rates["USDBRL"]["bid"].Value<decimal>();
                    _eurRate = rates["EURBRL"]["bid"].Value<decimal>();
                    _arsRate = rates["ARSBRL"]["bid"].Value<decimal>();

                    // Update the UI with the initial currency rate (BRL by default)
                    AtualizarValorMoeda("BRL", 1);
                }
                else
                {
                    MessageBox.Show("Não foi possível carregar as taxas de câmbio.");
                }
            }
        }

        private void AtualizarValorMoeda(string moeda, decimal rate)
        {
            _selectedRate = rate;
            txtValorMoeda.Text = $"1 {moeda} = {_selectedRate:N2} BRL";
            CalcularTotais(); // Recalculate totals with the new rate
        }

        private void Adicionar_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtDescricao.Text) &&
                int.TryParse(txtQuantidade.Text, out int quantidade) &&
                decimal.TryParse(txtPreco.Text, out decimal preco))
            {
                Produtos.Add(new Produto
                {
                    Descricao = txtDescricao.Text,
                    Quantidade = quantidade,
                    Preco = preco
                });

                CalcularTotais();
                LimparCampos();
            }
            else
            {
                MessageBox.Show("Por favor, preencha todos os campos corretamente.");
            }
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (lvProdutos.SelectedItem != null)
            {
                var produtoSelecionado = (Produto)lvProdutos.SelectedItem;

                // Preencher os campos de adição com os valores do produto selecionado
                txtDescricao.Text = produtoSelecionado.Descricao;
                txtQuantidade.Text = produtoSelecionado.Quantidade.ToString();
                txtPreco.Text = produtoSelecionado.Preco.ToString();

                // Remover o item selecionado da lista
                Produtos.Remove(produtoSelecionado);

                CalcularTotais();
            }
            else
            {
                MessageBox.Show("Por favor, selecione um produto para editar.");
            }
        }

        private void Excluir_Click(object sender, RoutedEventArgs e)
        {
            if (lvProdutos.SelectedItem != null)
            {
                var produtoSelecionado = (Produto)lvProdutos.SelectedItem;
                Produtos.Remove(produtoSelecionado);
                CalcularTotais();
                
            }
            else
            {
                MessageBox.Show("Por favor, selecione um produto para excluir.");
            }
        }

        private void AplicarDesconto_Click(object sender, RoutedEventArgs e)
        {
            CalcularTotais();
            
        }

        private void CalcularTotais()
        {
            decimal total = 0;
            int quantidadeTotal = 0;

            foreach (var produto in Produtos)
            {
                total += produto.Preco * produto.Quantidade;
                quantidadeTotal += produto.Quantidade;
            }

            // Aplicar desconto, se houver
            if (decimal.TryParse(txtDesconto.Text, out decimal desconto))
            {
                total *= (1 - desconto / 100);
            }

            // Converter para a moeda selecionada
            decimal totalEmMoeda = total / _selectedRate;

            txtTotal.Text = $"Total: {totalEmMoeda+frete:N2}";
            txtQuantidadeTotal.Text = $"Quantidade Total: {quantidadeTotal}";
            txtVolumeTotal.Text = $"Volume Total: {total+frete:N2} BRL";
            CalcularImpostos(total+frete);
        }

        private void CalcularImpostos(decimal i)
        {
            decimal imposto = 0;
            decimal totalImpostos = 0;
            decimal aliquotaImportacao = 0;

            // Exemplo de cálculo de impostos fictícios
            foreach (var produto in Produtos)
            {
                imposto += produto.Preco * 0.1m; // 10% do preço como imposto
                totalImpostos += produto.Preco * 0.15m; // 15% do preço como total de impostos
                aliquotaImportacao += produto.Preco * 0.92m; // 92% do preço como alíquota de importação
            }

            // Atualize os valores nos elementos visuais correspondentes
            txtAliquotaImportacao.Text = $"92%";
            txtTotalDosItens.Text = $"{ aliquotaImportacao + i:N2} BRL";
        }


        private void lvProdutos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Aqui você pode adicionar lógica que precisa ser executada quando a seleção muda
        }

        private void LimparCampos()
        {
            txtDescricao.Text = string.Empty;
            txtQuantidade.Text = string.Empty;
            txtPreco.Text = string.Empty;
        }

        private void cbMoeda_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbMoeda.SelectedItem != null)
            {
                string moeda = ((System.Windows.Controls.ComboBoxItem)cbMoeda.SelectedItem).Content.ToString();
                switch (moeda)
                {
                    case "BRL":
                        AtualizarValorMoeda(moeda, 1);
                        break;
                    case "USD":
                        AtualizarValorMoeda(moeda, _usdRate);
                        break;
                    case "EUR":
                        AtualizarValorMoeda(moeda, _eurRate);
                        break;
                    case "ARS":
                        AtualizarValorMoeda(moeda, _arsRate);
                        break;
                }
            }
        }
       
        private void AplicarFrete_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtFrete.Text, out decimal fret))
            {
                frete = fret;
                CalcularTotais();
            }
            else
            {
                // Se a conversão falhar, você pode lidar com isso aqui
            }
        }



    }

    public class Produto
    {
        public string Descricao { get; set; }
        public int Quantidade { get; set; }
        public decimal Preco { get; set; }
    }
}