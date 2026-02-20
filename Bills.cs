using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using I386API;

namespace BillsOnline;

internal class Bills {

    private List<IBill> bills;

    private BillsState state;
    private int index;

    int totalBytesDownloaded;
    Coroutine routine;

    private bool reconnect;
    private bool downloaded;

    private FsmFloat bankAccount;

    public void load() {
        // Load diskette texture
        Texture2D texture = new Texture2D(128, 128);
        texture.LoadImage(Properties.Resources.FLOPPY_FINES);
        texture.name = "FLOPPY_BILLS";

        // Create command
        Command command = Command.Create("bills", command_enter, command_update);
        
        // Create diskette
        Diskette diskette = Diskette.Create("bills", new Vector3(-10.39007f, 0.2121807f, 14.01701f), new Vector3(270f, 198.4409f, 0f));
        diskette.SetTexture(texture);

        Transform t1 = GameObject.Find("Systems/PhoneBills1").transform;
        PhoneBill phoneBill1 = new PhoneBill();
        phoneBill1.load("Phone Bill 1", t1);

        Transform t2 = GameObject.Find("Systems/PhoneBills2").transform;
        PhoneBill phoneBill2 = new PhoneBill();
        phoneBill2.load("Phone Bill 2", t2);

        Transform t3 = GameObject.Find("Systems/ElectricityBills1").transform;
        ElectricityBill electricityBill1 = new ElectricityBill();
        electricityBill1.load("Electricity Bill 1", t3);

        Transform t4 = GameObject.Find("Systems/ElectricityBills2").transform;
        ElectricityBill electricityBill2 = new ElectricityBill();
        electricityBill2.load("Electricity Bill 2", t4);

        bills = new List<IBill>();
        bills.Add(phoneBill1);
        bills.Add(electricityBill1);
        bills.Add(phoneBill2);
        bills.Add(electricityBill2);

        bankAccount = PlayMakerGlobals.Instance.Variables.GetFsmFloat("PlayerBankAccount");
    }

    private IEnumerator processPaymentAsync() {
        if (I386.ModemConnected) {
            if (reconnect) {
                state = BillsState.Connect;
            }
            else {
                yield return new WaitForSeconds(0.65f);
                if (payBill(bills[index])) {
                    state = BillsState.PaymentSuccess;
                }
                else {
                    state = BillsState.PaymentFailed;
                }
            }
        }
        else {
            yield return new WaitForSeconds(1.3f);
            state = BillsState.NotConnected;
        }
        routine = null;
    }
    private IEnumerator paymentSuccessAsync() {
        yield return new WaitForSeconds(1.3f);
        state = BillsState.Viewing;
        routine = null;
    }
    private IEnumerator paymentFailedAsync() {
        yield return new WaitForSeconds(1.3f);
        state = BillsState.Viewing;
        routine = null;
    }
    private IEnumerator connectAsync() {
        if (I386.ModemConnected) {
            yield return new WaitForSeconds(0.65f);
            state = BillsState.Viewing;
        }
        else {
            yield return new WaitForSeconds(1.3f);
            state = BillsState.NotConnected;
        }

        reconnect = false;
        routine = null;
    }

    private void processPayment() {
        if (routine == null) {
            routine = I386.StartCoroutine(processPaymentAsync());
        }
    }
    private void paymentSuccess() {
        if (routine == null) {
            routine = I386.StartCoroutine(paymentSuccessAsync());
        }
    }
    private void paymentFailed() {
        if (routine == null) {
            routine = I386.StartCoroutine(paymentFailedAsync());
        }
    }
    private void connect() {
        if (routine == null) {
            routine = I386.StartCoroutine(connectAsync());
        }
    }
    private bool payBill(IBill bill) {
        if (bankAccount.Value >= bill.getPrice()) {
            bankAccount.Value -= bill.getPrice();
            bill.pay();
            return true;
        }
        else {
            return false;
        }
    }
    private void viewHeader() {
        I386.POS_ClearScreen();
        I386.POS_WriteNewLine("                                   Bills Online");
        I386.POS_WriteNewLine("--------------------------------------------------------------------------------");
    }

    private void viewBill(IBill bill) {
        if (I386.GetKeyDown(KeyCode.LeftArrow)) {
            index = index - 1;
            if (index < 0) {
                index = bills.Count - 1;
            }
        }
        if (I386.GetKeyDown(KeyCode.RightArrow)) {
            index = index + 1;
            if (index >= bills.Count) {
                index = 0;
            }
        }

        if (I386.ModemConnected) {
            if (reconnect) {
                state = BillsState.Connect;
                return;
            }
        }
        else {
            reconnect = true;
        }

        viewHeader();

        I386.POS_WriteNewLine($"   {bill.name}\n");
        I386.POS_WriteNewLine($"   Definition\t\t\t\t\t\tQuantity\t\tPrice MK\t\tTotal MK\n");

        if (bill.unpaidAmount > 1) {
            bill.view();

            if (I386.GetKeyDown(KeyCode.Space)) {
                state = BillsState.PaymentProcessing;
                return;
            }

            I386.POS_Write($"\t\t\t\t\t\t\t\t[PAY NOW] ");
            if (bill.timeUntilCutOff <= 0) {
                I386.POS_WriteNewLine($"Overdue");
            }
            else {
                I386.POS_WriteNewLine($"Due: {bill.timeUntilCutOff}");
            }
        }
        else {
            I386.POS_WriteNewLine($"\t\t\t\t\t\t\tInvoice not ready. Due: {bill.timeUntilNextBill}");
        }
    }
    private void viewNotConnected() {
        viewHeader();
        I386.POS_WriteNewLine("                                   Bills Online");
        I386.POS_WriteNewLine("--------------------------------------------------------------------------------");
        I386.POS_WriteNewLine($"                                  Not Connected");
        I386.POS_WriteNewLine($"                              Press Space to Connect");
        if (I386.GetKeyDown(KeyCode.Space)) {
            state = BillsState.Connect;
        }
    }
    private void viewConnect() {
        viewHeader();
        I386.POS_WriteNewLine($"                                  Connecting...");
        connect();
    }
    private void viewPaymentProcessing() {
        viewHeader();
        I386.POS_WriteNewLine($"                                Processing Payment...");
        processPayment();
    }
    private void viewPaymentSuccess() {
        viewHeader();
        I386.POS_WriteNewLine($"                                 Payment Success");
        paymentSuccess();
    }
    private void viewPaymentFailed() {
        viewHeader();
        I386.POS_WriteNewLine($"                                 Payment Failed");
        paymentFailed();
    }

    private bool command_enter() {
        index = 0;
        state = BillsState.Connect;
        return false; // do update
    }
    private bool command_update() {
        if ((I386.GetKey(KeyCode.LeftControl) || I386.GetKey(KeyCode.RightControl)) && I386.GetKeyDown(KeyCode.C)) {
            return true; // exit
        }

        switch (state) {
            case BillsState.Connect:
                viewConnect();
                break;
            case BillsState.NotConnected:
                viewNotConnected();
                break;
            case BillsState.Viewing:
                viewBill(bills[index]);
                break;
            case BillsState.PaymentProcessing:
                viewPaymentProcessing();
                break;
            case BillsState.PaymentSuccess:
                viewPaymentSuccess();
                break;
            case BillsState.PaymentFailed:
                viewPaymentFailed();
                break;
        }

        return false; // continue
    }
}
