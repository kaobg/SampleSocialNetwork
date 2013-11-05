$(function() {

	function usersViewModel() {
		var self = this;
		self.UserId;
		self.RelationName = ko.observable();
		self.hub = $.connection.usersHub;


		self.init = function() {
			self.UserId = $("#userId").val();

			self.hub.server.initialize(self.UserId).done(function(data) {
				self.RelationName(data.RelationName);
			});
		}
	}

	var vm = new usersViewModel();
	ko.applyBindings(vm, document.getElementById("profileInfoHolder"));

	window.hubReady.done(function() {
		vm.init();
	});

});