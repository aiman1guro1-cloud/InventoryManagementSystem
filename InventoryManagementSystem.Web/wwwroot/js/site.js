// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Update cart count
function updateCartCount() {
    fetch('/Cart/GetCartSummary')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('cartCount');
            if (data.totalItems > 0) {
                badge.textContent = data.totalItems;
                badge.style.display = 'inline';
            } else {
                badge.style.display = 'none';
            }
        });
}

// Update on page load
document.addEventListener('DOMContentLoaded', updateCartCount);