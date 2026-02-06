window.paymentInterop = {
    tossPayments: null,
    clientKey: null,

    // Initialize (Load SDK)
    initPaymentWidget: function (clientKey, customerKey) {
        // We use the same function name for compatibility, but internally we init standard payments
        this.clientKey = clientKey;
        this.tossPayments = TossPayments(clientKey);
        console.log("TossPayments Initialized (Standard Mode)");
    },

    // Not used in Standard Mode, but kept to prevent errors if called
    renderPaymentMethodWithCallback: function (dotNetRef, selector, amount) {
        console.warn("Standard Mode: renderPaymentMethods ignored.");
        // Immediately signal success
        setTimeout(function () { dotNetRef.invokeMethodAsync('OnWidgetRendered'); }, 100);
    },

    renderAgreement: function (selector) { },

    // Direct Payment Request (Standard Window)
    requestPayment: function (orderId, orderName, amount, customerEmail, customerName) {
        if (!this.tossPayments) {
            // Lazy Init if needed
            if (this.clientKey) this.tossPayments = TossPayments(this.clientKey);
            else return Promise.reject("Not initialized");
        }

        // Standard Payment Request (Card)
        return this.tossPayments.requestPayment('카드', {
            amount: amount,
            orderId: orderId,
            orderName: orderName,
            successUrl: "http://localhost/payment/success",
            failUrl: "http://localhost/payment/fail",
            customerEmail: customerEmail,
            customerName: customerName
        });
    }
};
