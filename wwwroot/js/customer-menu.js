document.addEventListener("DOMContentLoaded", function () {

    "use strict";


    // =========================================================
    // ELEMENTS
    // =========================================================

    const forms =
        document.querySelectorAll(
            ".add-to-cart-form"
        );

    const stickyCart =
        document.getElementById(
            "sticky-cart-bar"
        );

    const cartCount =
        document.getElementById(
            "sticky-cart-count"
        );

    const cartItemLabel =
        document.getElementById(
            "sticky-cart-item-label"
        );

    const cartTotal =
        document.getElementById(
            "sticky-cart-total"
        );

    const toast =
        document.getElementById(
            "menu-toast"
        );

    const toastMessage =
        document.getElementById(
            "menu-toast-message"
        );

    const categoryDock =
        document.getElementById(
            "categoryDock"
        );

    const categoryLinks =
        Array.from(
            document.querySelectorAll(
                ".rq-category-pill"
            )
        );

    const categorySections =
        Array.from(
            document.querySelectorAll(
                "[data-category-section]"
            )
        );


    let toastTimer = null;



    // =========================================================
    // NUMBER FORMATTER
    // =========================================================

    const moneyFormatter =
        new Intl.NumberFormat(
            "en-IN",
            {
                maximumFractionDigits: 2
            }
        );



    // =========================================================
    // TOAST
    // =========================================================

    function showToast(message) {

        if (!toast) {
            return;
        }


        if (toastMessage) {

            toastMessage.textContent =
                message || "Added to cart";
        }


        toast.classList.add(
            "show"
        );


        if (toastTimer) {

            clearTimeout(
                toastTimer
            );
        }


        toastTimer =
            setTimeout(
                function () {

                    toast.classList.remove(
                        "show"
                    );

                },
                1800
            );
    }



    // =========================================================
    // CART ITEM LABEL
    // =========================================================

    function updateCartLabel(quantity) {

        if (!cartItemLabel) {
            return;
        }


        cartItemLabel.textContent =
            Number(quantity) === 1
                ? "item"
                : "items";
    }



    // =========================================================
    // UPDATE STICKY CART
    // =========================================================

    function updateCart(
        quantity,
        total
    ) {

        if (cartCount) {

            cartCount.textContent =
                Number(quantity || 0)
                    .toLocaleString(
                        "en-IN"
                    );
        }


        updateCartLabel(
            quantity
        );


        if (cartTotal) {

            cartTotal.textContent =
                moneyFormatter.format(
                    Number(total || 0)
                );
        }


        if (stickyCart) {

            stickyCart.classList.add(
                "rq-cart-pulse"
            );


            setTimeout(
                function () {

                    stickyCart.classList.remove(
                        "rq-cart-pulse"
                    );

                },
                350
            );
        }
    }



    // =========================================================
    // SERVING OPTION PRICE
    // =========================================================

    document
        .querySelectorAll(
            ".rq-menu-card"
        )
        .forEach(
            function (card) {

                const radios =
                    card.querySelectorAll(
                        'input[name="optionId"]'
                    );

                const priceDisplay =
                    card.querySelector(
                        "[data-selected-price]"
                    );


                if (
                    !radios.length ||
                    !priceDisplay
                ) {
                    return;
                }


                radios.forEach(
                    function (radio) {

                        radio.addEventListener(
                            "change",
                            function () {

                                const label =
                                    card.querySelector(
                                        `label[for="${radio.id}"]`
                                    );


                                if (!label) {
                                    return;
                                }


                                const optionPrice =
                                    label.querySelector(
                                        ".rq-serving-option-price"
                                    );


                                if (!optionPrice) {
                                    return;
                                }


                                priceDisplay.textContent =
                                    optionPrice.textContent.trim();
                            }
                        );
                    }
                );
            }
        );



    // =========================================================
    // ADD TO CART
    // =========================================================

    forms.forEach(
        function (form) {

            form.addEventListener(
                "submit",
                async function (event) {

                    event.preventDefault();


                    // -----------------------------------------
                    // NORMAL HTML FORM VALIDATION
                    // -----------------------------------------

                    if (!form.reportValidity()) {
                        return;
                    }


                    const button =
                        form.querySelector(
                            ".rq-menu-add-button"
                        );

                    const buttonLabel =
                        button
                            ? button.querySelector(
                                ".menu-add-label"
                            )
                            : null;

                    const buttonIcon =
                        button
                            ? button.querySelector(
                                ".menu-add-icon"
                            )
                            : null;

                    const itemName =
                        form.dataset.itemName ||
                        "Item";


                    // -----------------------------------------
                    // DISABLE BUTTON WHILE REQUEST IS RUNNING
                    // -----------------------------------------

                    if (button) {
                        button.disabled = true;
                    }


                    if (buttonLabel) {

                        buttonLabel.textContent =
                            "Adding";
                    }


                    if (buttonIcon) {

                        buttonIcon.textContent =
                            "…";
                    }


                    try {

                        // =====================================
                        // CREATE FORM DATA
                        // =====================================

                        const formData =
                            new FormData(
                                form
                            );


                        // =====================================
                        // SEND AJAX REQUEST
                        // =====================================

                        const response =
                            await fetch(
                                form.action,
                                {
                                    method: "POST",

                                    body:
                                        formData,

                                    headers: {

                                        "X-Requested-With":
                                            "XMLHttpRequest",

                                        "Accept":
                                            "application/json"
                                    },

                                    credentials:
                                        "same-origin"
                                }
                            );


                        // =====================================
                        // READ SERVER RESPONSE
                        // =====================================

                        const responseText =
                            await response.text();


                        let result = null;


                        try {

                            result =
                                responseText
                                    ? JSON.parse(
                                        responseText
                                    )
                                    : null;

                        }
                        catch (jsonError) {

                            console.error(
                                "INVALID JSON RESPONSE:",
                                responseText
                            );


                            throw new Error(
                                "The server returned an unexpected response."
                            );
                        }


                        // =====================================
                        // HTTP ERROR
                        // =====================================

                        if (!response.ok) {

                            const message =
                                result &&
                                    result.message
                                    ? result.message
                                    : "Unable to add this item.";


                            alert(
                                message
                            );


                            return;
                        }


                        // =====================================
                        // APPLICATION ERROR
                        // =====================================

                        if (
                            !result ||
                            !result.success
                        ) {

                            alert(
                                result &&
                                    result.message
                                    ? result.message
                                    : "Unable to add this item."
                            );


                            return;
                        }


                        // =====================================
                        // UPDATE FIXED CART
                        // =====================================

                        updateCart(
                            result.totalQuantity,
                            result.totalPrice
                        );


                        // =====================================
                        // SUCCESS BUTTON FEEDBACK
                        // =====================================

                        if (buttonLabel) {

                            buttonLabel.textContent =
                                "Added";
                        }


                        if (buttonIcon) {

                            buttonIcon.textContent =
                                "✓";
                        }


                        // =====================================
                        // SHOW TOAST
                        // =====================================

                        showToast(
                            `${itemName} added to cart`
                        );


                        // =====================================
                        // RESET BUTTON
                        // =====================================

                        setTimeout(
                            function () {

                                if (buttonLabel) {

                                    buttonLabel.textContent =
                                        "Add";
                                }


                                if (buttonIcon) {

                                    buttonIcon.textContent =
                                        "+";
                                }

                            },
                            900
                        );

                    }
                    catch (error) {

                        // =====================================
                        // REQUEST / SERVER / JS ERROR
                        // =====================================

                        console.error(
                            "ADD TO CART ERROR:",
                            error
                        );


                        alert(
                            "Could not add the item. " +
                            (
                                error &&
                                    error.message
                                    ? error.message
                                    : ""
                            )
                        );


                        if (buttonLabel) {

                            buttonLabel.textContent =
                                "Add";
                        }


                        if (buttonIcon) {

                            buttonIcon.textContent =
                                "+";
                        }
                    }
                    finally {

                        // =====================================
                        // ENABLE BUTTON AGAIN
                        // =====================================

                        if (button) {

                            setTimeout(
                                function () {

                                    button.disabled =
                                        false;

                                },
                                500
                            );
                        }
                    }
                }
            );
        }
    );



    // =========================================================
    // CATEGORY CLICK
    // =========================================================

    categoryLinks.forEach(
        function (link) {

            link.addEventListener(
                "click",
                function (event) {

                    const href =
                        link.getAttribute(
                            "href"
                        );


                    if (
                        !href ||
                        !href.startsWith("#")
                    ) {
                        return;
                    }


                    const target =
                        document.querySelector(
                            href
                        );


                    if (!target) {
                        return;
                    }


                    event.preventDefault();


                    // -----------------------------------------
                    // GET CUSTOMER HEADER HEIGHT
                    // -----------------------------------------

                    const header =
                        document.querySelector(
                            ".customer-header"
                        );


                    const headerHeight =
                        header
                            ? header.offsetHeight
                            : 0;


                    // -----------------------------------------
                    // GET CATEGORY DOCK HEIGHT
                    // -----------------------------------------

                    const dockHeight =
                        categoryDock
                            ? categoryDock.offsetHeight
                            : 0;


                    // -----------------------------------------
                    // SCROLL TARGET POSITION
                    // -----------------------------------------

                    const top =
                        target
                            .getBoundingClientRect()
                            .top +

                        window.scrollY -

                        headerHeight -

                        dockHeight -

                        18;


                    window.scrollTo({

                        top:
                            Math.max(
                                top,
                                0
                            ),

                        behavior:
                            "smooth"
                    });


                    setActiveCategory(
                        link
                    );
                }
            );
        }
    );



    // =========================================================
    // SET ACTIVE CATEGORY
    // =========================================================

    function setActiveCategory(
        activeLink
    ) {

        categoryLinks.forEach(
            function (link) {

                link.classList.toggle(
                    "active",
                    link === activeLink
                );
            }
        );


        // -----------------------------------------
        // KEEP ACTIVE PILL VISIBLE
        // -----------------------------------------

        if (
            activeLink &&
            typeof activeLink.scrollIntoView ===
            "function"
        ) {

            activeLink.scrollIntoView({

                behavior:
                    "smooth",

                block:
                    "nearest",

                inline:
                    "center"
            });
        }
    }



    // =========================================================
    // AUTO ACTIVE CATEGORY WHILE SCROLLING
    // =========================================================

    if (
        categorySections.length &&
        categoryLinks.length &&
        "IntersectionObserver" in window
    ) {

        const observer =
            new IntersectionObserver(
                function (entries) {

                    // -----------------------------------------
                    // GET CURRENTLY VISIBLE SECTIONS
                    // -----------------------------------------

                    const visible =
                        entries
                            .filter(
                                function (entry) {

                                    return entry.isIntersecting;
                                }
                            )
                            .sort(
                                function (a, b) {

                                    return (
                                        b.intersectionRatio -
                                        a.intersectionRatio
                                    );
                                }
                            );


                    if (!visible.length) {
                        return;
                    }


                    // -----------------------------------------
                    // FIND ACTIVE CATEGORY INDEX
                    // -----------------------------------------

                    const index =
                        visible[0]
                            .target
                            .dataset
                            .categorySection;


                    const activeLink =
                        document.querySelector(
                            `.rq-category-pill[data-category-index="${index}"]`
                        );


                    if (activeLink) {

                        setActiveCategory(
                            activeLink
                        );
                    }

                },
                {
                    root:
                        null,

                    rootMargin:
                        "-145px 0px -58% 0px",

                    threshold: [
                        0,
                        0.1,
                        0.25,
                        0.5
                    ]
                }
            );


        categorySections.forEach(
            function (section) {

                observer.observe(
                    section
                );
            }
        );
    }



    // =========================================================
    // INITIAL ACTIVE CATEGORY
    // =========================================================

    if (categoryLinks.length) {

        const currentActive =
            categoryLinks.find(
                function (link) {

                    return link.classList.contains(
                        "active"
                    );
                }
            );


        if (currentActive) {

            currentActive.scrollIntoView({

                behavior:
                    "auto",

                block:
                    "nearest",

                inline:
                    "start"
            });
        }
    }

});