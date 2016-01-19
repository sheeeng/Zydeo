$(document).ready(function () {
  // Add tooltips in desktop version only
  if (!isMobile) {
    $("#img-write").tooltipster({
      content: $("<span>" + uiArr["tooltip-btn-brush"] + "</span>")
    });
    $("#img-search").tooltipster({
      content: $("<span>" + uiArr["tooltip-btn-search"] + "</span>")
    });
  }

  initStrokes();

  // Debug: to work on strokes input
  //showStrokeInput();

  lookupEventWireup();

  // Do not focus input field on mobile: shows keyboard, annoying
  if (!isMobile) {
    $("#txtSearch").focus();
    $("#txtSearch").select();
  }

  // Debug: to work on opening screen
  //$("#resultsHolder").css("display", "none");
  //$("#welcomeScreen").css("display", "block");
});

// Shows the handwriting recognition pop-up.
function showStrokeInput() {
  if (!isMobile) {
    var searchPanelOfs = $("#search-panel").offset();
    var searchPanelWidth = $("#search-panel").width();
    var searchPanelHeight = $("#search-panel").height();
    var strokeInputWidth = $("#stroke-input").outerWidth();
    $("#stroke-input").css("top", searchPanelOfs.top + searchPanelHeight + 1);
    $("#stroke-input").css("left", searchPanelOfs.left + searchPanelWidth - strokeInputWidth + 2);
    $("#stroke-input").css("display", "block");
    $("#suggestions").html("<br/><br/>");
  }
  else {
    $("#stroke-input").css("display", "block");
    $("#suggestions").html("<br/>");
  }
  var strokeCanvasWidth = $("#stroke-input-canvas").width();
  $("#stroke-input-canvas").css("height", strokeCanvasWidth);
  var canvasElement = document.getElementById("stroke-input-canvas");
  canvasElement.width = strokeCanvasWidth;
  canvasElement.height = strokeCanvasWidth;
  $("#suggestions").css("height", $("#suggestions").height());
  clearCanvas();
  $("#btn-write").attr("class", "active");
  // Must do this explicitly: if hamburger menu is shown, got to hide it
  if (isMobile) hideShowHamburger(false);
}

// Hides the handwriting recognition popup
function hideStrokeInput() {
  // Nothing to hide?
  if ($("#stroke-input").css("display") != "block") return;
  // Hide.
  $("#stroke-input").css("display", "none");
  $("#btn-write").attr("class", "");
}

// Clears the search field
function clearSearch() {
  $("#txtSearch").val("");
  $("#txtSearch").focus();
}

// Submits a dictionary search as a POST request.
function submitSearch() {
  'use strict';
  var form;
  form = $('<form />', {
    action: '/',
    method: 'post',
    style: 'display: none;'
  });
  $('<input />', {
    type: 'hidden',
    name: 'query',
    value: $('#txtSearch').val()
  }).appendTo(form);
  $('<input />', {
    type: 'hidden',
    name: 'mobile',
    value: isMobile ? "yes" : "no"
  }).appendTo(form);
  form.appendTo('body').submit();
}

// Positions and shows the stroke animation pop-up for the clicked hanzi.
function hanziClicked(event) {
  // We get click event when mouse button is released after selecting a single hanzi
  // Don't want to show pop-up in this edge case
  var sbe = getSelBoundElm();
  if (sbe != null && sbe.textContent == $(this).text())
    return;
  // OK, so we're actually showing. Stop propagation so we don't get auto-hidden.
  event.stopPropagation();
  // If previous show's in progress, kill it
  // Also kill stroke input, in case it's shown
  soaKill();
  hideStrokeInput();
  // Start the whole spiel
  // First, decide if we're showing box to left or right of character
  $("#soaBox").css("display", "block");
  var hanziOfs = $(this).offset();
  var onRight = hanziOfs.left < $(document).width() / 2;
  var left = hanziOfs.left + $(this).width() + 20;
  if (onRight) $("#soaBox").removeClass("soaBoxLeft");
  else {
    $("#soaBox").addClass("soaBoxLeft");
    left = hanziOfs.left - $("#soaBox").width() - 20;
  }
  // Decide about Y position. Box wants char to be at vertical middle
  // But is willing to move up/down to fit in content area
  var charY = hanziOfs.top + $(this).height() / 2;
  var boxH = $("#soaBox").height();
  var top = charY - boxH / 2;
  // First, nudge up if we stretch beyond viewport bottom
  var wBottom = window.pageYOffset + window.innerHeight - 10;
  if (top + boxH > wBottom) top = wBottom - boxH;
  // Then, nudge down if we're over the ceiling
  var wTop = $("#search-bar").position().top + $("#search-bar").height() + window.pageYOffset + 20;
  if (top < wTop) top = wTop;
  // Position box, and tail
  $("#soaBox").offset({ left: left, top: top });
  $("#soaBoxTail").css("top", (charY - top - 10) + "px");
  // Render grid, issue AJAX query for animation data
  soaRenderBG();
  soaStartQuery($(this).text());
}

// Closes the stroke animation pop-up (if shown).
function closeStrokeAnim() {
  soaKill();
  $("#soaBox").css("display", "none");
}

function lookupEventWireup() {
  $("#btn-clear").click(clearSearch);
  $("#btn-write").click(function () {
    if ($("#stroke-input").css("display") == "block") hideStrokeInput();
    else  showStrokeInput();
  });
  // Auto-hide stroke input when tapping away
  // Also for stroke animation pop-up
  $('html').click(function () {
    hideStrokeInput();
    closeStrokeAnim();
  });
  // Must do explicitly for hamburger menu, b/c that stops event propagation
  if (isMobile) $('#btn-menu').click(function () {
    hideStrokeInput();
    closeStrokeAnim();
  });
  $('#btn-write').click(function (event) {
    event.stopPropagation();
    closeStrokeAnim();
  });
  $('#stroke-input').click(function (event) {
    event.stopPropagation();
  });

  $("#strokeClear").click(clearCanvas);
  $("#strokeUndo").click(undoStroke);
  $("#btn-search").click(submitSearch);
  $("#txtSearch").keyup(function (e) {
    if (e.keyCode == 13) {
      submitSearch();
      return false;
    }
  });
  $("#txtSearch").change(function () {
    appendNotOverwrite = true;
  });

  $(".hanim").click(hanziClicked);
  $("#soaClose").click(closeStrokeAnim);
  $("#soaBox").click(function (e) {
    e.stopPropagation();
  });
}
