using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace QuanLyQuanCafe
{
    public partial class fBan : Form
    {
        private string apiUrl =
            "https://6a00a85436fb6ad04de05cca.mockapi.io/fBan";

        private List<CafeTable> allTables = new List<CafeTable>();
        private bool isSelecting = false;

        public fBan()
        {
            InitializeComponent();
            this.Load += async (s, e) => await LoadTables();
            dgvTables.SelectionChanged += dgvTables_SelectionChanged;
            button1.Click += (s, e) => Close();                            // button1 = Đóng
            button2.Click += button2_Search_Click;                         // button2 = Tìm kiếm
            button3.Click += async (s, e) => await button3_Update_Click(); // button3 = Cập nhật
            button4.Click += async (s, e) => await button4_Delete_Click(); // button4 = Xóa
            button5.Click += async (s, e) => await button5_Add_Click();    // button5 = Thêm mới

            comboBox2.Items.AddRange(new string[] { "Đang sử dụng", "Trống" });
            comboBox3.Items.AddRange(new string[] { "Đã đặt trước", "Chưa đặt" });
        }

        // ==================== MODEL ====================
        public class CafeTable
        {
            public string id { get; set; }
            public int tableid { get; set; }
            public string tablename { get; set; }
            public bool status { get; set; }
            public bool reserved { get; set; }
        }

        // ==================== LOAD DỮ LIỆU ====================
        private async Task LoadTables()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    allTables = JsonConvert.DeserializeObject<List<CafeTable>>(response);
                    dgvTables.DataSource = null;
                    dgvTables.DataSource = allTables;
                    CustomizeGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LỖI tải dữ liệu: " + ex.Message);
            }
        }

        // ==================== CẤU HÌNH CỘT ====================
        private void CustomizeGrid()
        {
            dgvTables.Columns["id"].HeaderText = "STT";
            dgvTables.Columns["tableid"].HeaderText = "Mã bàn";
            dgvTables.Columns["tablename"].HeaderText = "Tên bàn";
            dgvTables.Columns["status"].HeaderText = "Trạng thái bàn";
            dgvTables.Columns["reserved"].HeaderText = "Đã đặt trước?";
        }

        // ==================== CLICK HÀNG TRÊN GRID ====================
        private void dgvTables_SelectionChanged(object sender, EventArgs e)
        {
            if (isSelecting) return;
            if (dgvTables.SelectedRows.Count == 0) return;

            CafeTable selected = dgvTables.SelectedRows[0].DataBoundItem as CafeTable;
            if (selected == null) return;

            isSelecting = true;
            textBox1.Text = selected.tableid.ToString();
            textBox2.Text = selected.tablename;
            comboBox2.Text = selected.status ? "Đang sử dụng" : "Trống";
            comboBox3.Text = selected.reserved ? "Đã đặt trước" : "Chưa đặt";
            isSelecting = false;
        }

        // ==================== BUTTON2: SEARCH ====================
        private void button2_Search_Click(object sender, EventArgs e)
        {
            string keyword = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Vui lòng nhập mã bàn cần tìm!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CafeTable found = allTables.FirstOrDefault(t => t.tableid.ToString() == keyword);

            if (found == null)
            {
                MessageBox.Show($"Không tìm thấy bàn có mã: {keyword}", "Không tìm thấy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            textBox1.Text = found.tableid.ToString();
            textBox2.Text = found.tablename;
            comboBox2.Text = found.status ? "Đang sử dụng" : "Trống";
            comboBox3.Text = found.reserved ? "Đã đặt trước" : "Chưa đặt";

            foreach (DataGridViewRow row in dgvTables.Rows)
            {
                CafeTable item = row.DataBoundItem as CafeTable;
                if (item != null && item.tableid.ToString() == keyword)
                {
                    dgvTables.ClearSelection();
                    row.Selected = true;
                    dgvTables.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // ==================== BUTTON3: CẬP NHẬT ====================
        private async Task button3_Update_Click()
        {
            string keyword = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Vui lòng tìm bàn trước khi cập nhật!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CafeTable found = allTables.FirstOrDefault(t => t.tableid.ToString() == keyword);

            if (found == null)
            {
                MessageBox.Show($"Không tìm thấy bàn có mã: {keyword}", "Không tìm thấy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            found.tablename = textBox2.Text.Trim();
            found.status = comboBox2.Text == "Đang sử dụng";
            found.reserved = comboBox3.Text == "Đã đặt trước";

            bool success = await UpdateTableApi(found);

            if (success)
            {
                dgvTables.DataSource = null;
                dgvTables.DataSource = allTables;
                CustomizeGrid();

                foreach (DataGridViewRow row in dgvTables.Rows)
                {
                    CafeTable item = row.DataBoundItem as CafeTable;
                    if (item != null && item.tableid.ToString() == keyword)
                    {
                        dgvTables.ClearSelection();
                        row.Selected = true;
                        dgvTables.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }

                MessageBox.Show("Cập nhật thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==================== BUTTON4: XÓA ====================
        private async Task button4_Delete_Click()
        {
            string keyword = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Vui lòng tìm bàn trước khi xóa!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CafeTable found = allTables.FirstOrDefault(t => t.tableid.ToString() == keyword);

            if (found == null)
            {
                MessageBox.Show($"Không tìm thấy bàn có mã: {keyword}", "Không tìm thấy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa bàn '{found.tablename}' không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            bool success = await DeleteTableApi(found);

            if (success)
            {
                allTables.Remove(found);
                dgvTables.DataSource = null;
                dgvTables.DataSource = allTables;
                CustomizeGrid();

                textBox1.Text = "";
                textBox2.Text = "";
                comboBox2.SelectedIndex = -1;
                comboBox3.SelectedIndex = -1;

                MessageBox.Show("Xóa thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==================== BUTTON5: THÊM MỚI ====================
        private async Task button5_Add_Click()
        {
            string keyword = textBox1.Text.Trim();
            string tablename = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Vui lòng nhập mã bàn!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(tablename))
            {
                MessageBox.Show("Vui lòng nhập tên bàn!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(keyword, out int tableid))
            {
                MessageBox.Show("Mã bàn phải là số!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kiểm tra trùng mã bàn
            bool isDuplicate = allTables.Any(t => t.tableid == tableid);
            if (isDuplicate)
            {
                MessageBox.Show($"Mã bàn {tableid} đã tồn tại! Vui lòng nhập mã khác.",
                    "Trùng mã bàn",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            CafeTable newTable = new CafeTable
            {
                tableid = tableid,
                tablename = tablename,
                status = comboBox2.Text == "Đang sử dụng",
                reserved = comboBox3.Text == "Đã đặt trước"
            };

            bool success = await AddTableApi(newTable);

            if (success)
            {
                await LoadTables();

                foreach (DataGridViewRow row in dgvTables.Rows)
                {
                    CafeTable item = row.DataBoundItem as CafeTable;
                    if (item != null && item.tableid == tableid)
                    {
                        dgvTables.ClearSelection();
                        row.Selected = true;
                        dgvTables.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }

                MessageBox.Show("Thêm bàn thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==================== GỌI API POST ====================
        private async Task<bool> AddTableApi(CafeTable table)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = JsonConvert.SerializeObject(table);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Lỗi thêm API: " + response.StatusCode);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LỖI gọi API: " + ex.Message);
                return false;
            }
        }

        // ==================== GỌI API PUT ====================
        private async Task<bool> UpdateTableApi(CafeTable table)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = JsonConvert.SerializeObject(table);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    string url = $"{apiUrl}/{table.id}";
                    HttpResponseMessage response = await client.PutAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Lỗi cập nhật API: " + response.StatusCode);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LỖI gọi API: " + ex.Message);
                return false;
            }
        }

        // ==================== GỌI API DELETE ====================
        private async Task<bool> DeleteTableApi(CafeTable table)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"{apiUrl}/{table.id}";
                    HttpResponseMessage response = await client.DeleteAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Lỗi xóa API: " + response.StatusCode);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LỖI gọi API: " + ex.Message);
                return false;
            }
        }

        // ==================== CÁC EVENT TRỐNG ====================
        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }

        private void fBan_Load(object sender, EventArgs e)
        {

        }

        private void dgvTables_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}