using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Configuration;
using System.Text;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
       
       
    }

    public static string Encrypt(string toEncrypt, bool useHashing)
    {
        /* 
         *  Reference to: Syed Moshiur - Software Developer
         *  http://www.codeproject.com/Articles/14150/Encrypt-and-Decrypt-Data-with-C
         *  
         */
        byte[] keyArray;
        byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

        System.Configuration.AppSettingsReader settingsReader =
                                            new AppSettingsReader();
        // Get the key from config file

        string key = (string)settingsReader.GetValue("SecurityKey",
                                                         typeof(String));
        
        //If hashing use get hashcode regards to your key
        if (useHashing)
        {
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice

            hashmd5.Clear();
        }
        else
            keyArray = UTF8Encoding.UTF8.GetBytes(key);

        TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
        //set the secret key for the tripleDES algorithm
        tdes.Key = keyArray;
        //mode of operation. there are other 4 modes.
        //We choose ECB(Electronic code Book)
        tdes.Mode = CipherMode.ECB;
        //padding mode(if any extra byte added)

        tdes.Padding = PaddingMode.PKCS7;

        ICryptoTransform cTransform = tdes.CreateEncryptor();
        //transform the specified region of bytes array to resultArray
        byte[] resultArray =
          cTransform.TransformFinalBlock(toEncryptArray, 0,
          toEncryptArray.Length);
        //Release resources held by TripleDes Encryptor
        tdes.Clear();
        //Return the encrypted data into unreadable string format
        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    public static string Decrypt(string cipherString, bool useHashing)
    {
        /* 
         *  Reference to: Syed Moshiur - Software Developer
         *  http://www.codeproject.com/Articles/14150/Encrypt-and-Decrypt-Data-with-C
         *  
         */
        byte[] keyArray;
        //get the byte code of the string

        byte[] toEncryptArray = Convert.FromBase64String(cipherString);

        System.Configuration.AppSettingsReader settingsReader =
                                            new AppSettingsReader();
        //Get your key from config file to open the lock!
        string key = (string)settingsReader.GetValue("SecurityKey",
                                                     typeof(String));

        if (useHashing)
        {
            //if hashing was used get the hash code with regards to your key
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //release any resource held by the MD5CryptoServiceProvider

            hashmd5.Clear();
        }
        else
        {
            //if hashing was not implemented get the byte code of the key
            keyArray = UTF8Encoding.UTF8.GetBytes(key);
        }

        TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
        //set the secret key for the tripleDES algorithm
        tdes.Key = keyArray;
        //mode of operation. there are other 4 modes. 
        //We choose ECB(Electronic code Book)

        tdes.Mode = CipherMode.ECB;
        //padding mode(if any extra byte added)
        tdes.Padding = PaddingMode.PKCS7;

        ICryptoTransform cTransform = tdes.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(
                             toEncryptArray, 0, toEncryptArray.Length);
        //Release resources held by TripleDes Encryptor                
        tdes.Clear();
        //return the Clear decrypted TEXT
        return UTF8Encoding.UTF8.GetString(resultArray);
    }

    public static bool VerifyMember(string MemClass, string VerCode)
    {
        
        //Decrypt string and convert to correct type
        VerCode = Decrypt(VerCode, false);
        string[] _VerCode = VerCode.Split(',');
        int _Salary = Convert.ToInt32(_VerCode[0]);
        DateTime _BirthDate = Convert.ToDateTime(_VerCode[1]);


        switch (MemClass)
        {
            case "none":
                goto default;
            case "Regular":
                if (_BirthDate < DateTime.Now.AddYears(-59)) 
                {
                   return true;
                } else   
                goto default;
            case "Silver":
                if (_BirthDate > DateTime.Now.AddYears(-59) && (_Salary > 61195))
                {
                    return true;
                }
                else 
                goto default;
            case "Gold":
                if (_BirthDate > DateTime.Now.AddYears(-59) && (_Salary > 74169))
                {
                    return true;
                }
                else 
                goto default;
            default:
                return false;
                
            
        }
        
    }

    protected void SearchMembers(object sender, EventArgs e)
    {
        grdvMem.DataSourceID = "SqlDataSource1";
        grdvMem.DataBind();
    }

    protected void btnVerify_Click(object sender, EventArgs e)
    {
        SqlConnection conn = null;
        SqlDataReader rdr = null;
        
        conn = new SqlConnection(
            @"Data Source=.\SQLEXPRESS; AttachDbFilename='|DataDirectory|\employees.mdf';
            Integrated Security=True; User Instance=True");
        
        
        try
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM membership WHERE MembershipId=@Id", conn);
            
            // Search Parameters
            cmd.Parameters.AddWithValue("@Id", txtVerifyId.Text);
            

            rdr = cmd.ExecuteReader();
            rdr.Read();
            string memclass = rdr.GetString(3);
            string vercode = rdr.GetString(4);

            if (VerifyMember(memclass, vercode))
            {
                lblVerify.Text = "Member verification successfull";
            } else
                lblVerify.Text = "Member verification failed";
        }
        finally
        {
            if (rdr != null)
            { rdr.Close(); }
        }
        if (conn != null)
        {
            conn.Close();
        }

    }

    protected void btnAdd_Click(object sender, EventArgs e)
    {
        SqlConnection conn = new SqlConnection(
            @"Data Source=.\SQLEXPRESS; AttachDbFilename='|DataDirectory|\employees.mdf';
            Integrated Security=True; User Instance=True");
        conn.Open();

        SqlCommand cmd = conn.CreateCommand();
        SqlTransaction transaction;
        transaction = conn.BeginTransaction("SampleTransaction");

        cmd.Connection = conn;
        cmd.Transaction = transaction;
        // Insertion Placeholders
        
        cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text);
        cmd.Parameters.AddWithValue("@LastName", txtLastName.Text);
        cmd.Parameters.AddWithValue("@MemClass", txtMemClass.Text);
        
        cmd.Parameters.AddWithValue("@BirthDate", DateTime.Parse(txtBirthDate.Text));
        cmd.Parameters.AddWithValue("@CurrDate", DateTime.Parse(DateTime.Now.ToString("dd'-'MM'-'yyyy")));
        cmd.Parameters.AddWithValue("@Salary", int.Parse(txtSalary.Text));
        
        // Verfication Code
        string VerCode = txtSalary.Text + "," + txtBirthDate.Text;
        VerCode = Encrypt(VerCode, false);
        cmd.Parameters.AddWithValue("@VerCode", VerCode);

        try
        {
            cmd.CommandText =
                "INSERT INTO MEMBERSHIP (FirstName,LastName,MembershipClass,VerificationCode,birth_date,salary) VALUES (@FirstName,@LastName,@MemClass,@VerCode,@BirthDate,@Salary)";
            cmd.ExecuteNonQuery();
            

            // Attempt to commit the transaction.
            transaction.Commit();
            lblError.Text = "User added successfully to database.";
        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;

            // Attempt to roll back the transaction. 
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex2)
            {
                // This catch block will handle any errors that may have occurred 
                // on the server that would cause the rollback to fail, such as 
                // a closed connection.
                lblError.Text = ex2.Message;
            }
        }

        conn.Close();
    }

    protected void display_charts(object sender, EventArgs e)
    {
        chtSalary.Visible = true;
        Chart1.Visible = true;
        Chart2.Visible = true;
    }

    protected void hide_charts(object sender, EventArgs e)
    {
        chtSalary.Visible = false;
        Chart1.Visible = false;
        Chart2.Visible = false;
    }
    
}