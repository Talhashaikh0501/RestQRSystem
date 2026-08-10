document.addEventListener("DOMContentLoaded", function () {

    const forms = document.querySelectorAll(".add-to-cart-form");

    forms.forEach(function (form) {

        form.addEventListener("submit", async function (event) {

            event.preventDefault();

            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const result = await response.json();

            if (!result.success) return;

            const stickyBar = document.getElementById("sticky-cart-bar");
            const count = document.getElementById("sticky-cart-count");
            const total = document.getElementById("sticky-cart-total");

            count.textContent = result.totalQuantity;
            total.textContent = Number(result.totalPrice || result.subtotal).toLocaleString('en-IN');

            if (result.totalQuantity > 0) {
                stickyBar.classList.add("show-cart");
            }
        });
    });
});