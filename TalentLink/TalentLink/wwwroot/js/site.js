// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {

    // Handle search form via AJAX
    $("#searchForm").submit(function (e) {
        e.preventDefault();

        $.ajax({
            url: $(this).attr("action"),
            type: "GET",
            data: $(this).serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                $("#jobsContainer").html(response);
            },
            error: function () {
                alert("Error loading jobs");
            }
        });
    });

    // Handle pagination links via AJAX (delegated because content reloads)
    $(document).on("click", ".job-page-link", function (e) {
        e.preventDefault();

        $.ajax({
            url: $(this).attr("href"),
            type: "GET",
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                $("#jobsContainer").html(response);
            },
            error: function () {
                alert("Error loading jobs");
            }
        });
    });

});
