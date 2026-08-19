document.addEventListener("DOMContentLoaded", function () {

    const forms = document.querySelectorAll(".add-to-cart-form");

    forms.forEach(function (form) {

        form.addEventListener("submit", async function (event) {

            event.preventDefault();

            const button =
                form.querySelector(".menu-add-button");

            if (button) {
                button.disabled = true;
                button.textContent = "Adding...";
            }

            try {

                const formData =
                    new FormData(form);

                const response = await fetch(
                    form.getAttribute("action"),
                    {
                        method: "POST",
                        body: formData,

                        headers: {
                            "X-Requested-With":
                                "XMLHttpRequest",

                            "Accept":
                                "application/json"
                        },

                        credentials: "same-origin"
                    }
                );

                const text =
                    await response.text();

                console.log(
                    "Cart response:",
                    response.status,
                    text
                );

                let result;

                try {
                    result = JSON.parse(text);
                }
                catch {
                    throw new Error(
                        "Server returned an unexpected response."
                    );
                }

                if (!response.ok) {

                    alert(
                        result.message ||
                        "Unable to add this item."
                    );

                    return;
                }

                if (!result.success) {

                    alert(
                        result.message ||
                        "Unable to add this item."
                    );

                    return;
                }

                // ==========================================
                // UPDATE CART BAR
                // ==========================================

                const stickyCart =
                    document.getElementById(
                        "sticky-cart-bar"
                    );

                const cartCount =
                    document.getElementById(
                        "sticky-cart-count"
                    );

                const cartTotal =
                    document.getElementById(
                        "sticky-cart-total"
                    );

                if (cartCount) {
                    cartCount.textContent =
                        result.totalQuantity;
                }

                if (cartTotal) {
                    cartTotal.textContent =
                        Number(
                            result.totalPrice || 0
                        ).toLocaleString("en-IN");
                }

                if (stickyCart) {
                    stickyCart.classList.add(
                        "show-cart"
                    );
                }

                // ==========================================
                // BUTTON FEEDBACK
                // ==========================================

                if (button) {

                    button.textContent =
                        "Added ✓";

                    setTimeout(function () {

                        button.textContent =
                            "Add";

                    }, 1000);
                }

            }
            catch (error) {

                console.error(
                    "ADD TO CART ERROR:",
                    error
                );

                alert(
                    "Could not add the item. " +
                    error.message
                );

            }
            finally {

                if (button) {

                    setTimeout(function () {

                        button.disabled = false;

                        if (
                            button.textContent ===
                            "Adding..."
                        ) {
                            button.textContent =
                                "Add";
                        }

                    }, 1000);
                }
            }
        });
    });
});