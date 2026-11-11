Feature: Group pre-order participants review

  The owner of a group reservation invites guests to add dishes, reviews all
  participants and their orders, and sees the consolidated totals.

  Background:
    Given the restaurant has a waiter account
    And the manager has set up a new location
    And the manager adds a main course dish to the location

  Rule: The owner sees everyone who joined and their selected dishes

    @level1 @ui @smoke
    Scenario: The owner reviews joined participants and their dishes on the storefront
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And a guest called "Maria" joins the group and adds a dish
      When the owner opens the group pre-order page
      Then the owner sees participants "Alex" and "Maria"
      And the grand total equals the sum of all participant orders

    @level1 @ui
    Scenario: The grand total updates dynamically when a guest modifies their cart
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds 1 dish
      When the owner opens the group pre-order page
      Then the owner sees "Alex" with 1 dish
      When the guest "Alex" adds 1 more dish to their cart
      And the owner refreshes the group pre-order review
      Then the owner sees "Alex" with 2 dishes
      And the grand total reflects the updated quantity

    @level1 @api
    Scenario: Participants who have not submitted carts are marked accordingly
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And a guest called "Olena" joins the group and adds a dish
      And the guest "Olena" submits their cart
      When the owner views the group sessions
      Then "Alex" is listed as still choosing
      And "Olena" is listed as submitted

    @level1 @api
    Scenario: A participant without dishes is listed in the group
      Given the customer creates a group reservation
      And a guest called "David" joins the group without adding dishes
      When the owner views the group sessions
      Then "David" is listed with no dishes and a total of 0

    @level1 @api
    Scenario: An empty group has no participants and zero total
      Given the customer creates a group reservation
      When the owner views the group sessions
      Then the group has no participants and a total of 0

  Rule: Only the reservation owner can view the group participants

    @level1 @api @negative
    Scenario: Another customer cannot view the group participants
      Given the customer creates a group reservation
      When another customer attempts to view the group sessions
      Then access to the group sessions is forbidden

    @level1 @api @negative
    Scenario: An unauthenticated user cannot view the group participants
      Given the customer creates a group reservation
      When an unauthenticated user attempts to view the group sessions
      Then viewing the group sessions is refused as unauthorized

    @level1 @api @negative
    Scenario: Requesting participants of a non-existent group returns not found
      When a customer attempts to view sessions of a non-existent group
      Then the group is reported as not found

    @level1 @ui @negative
    Scenario: Opening a non-existent group pre-order on the storefront shows an error
      When the customer opens the group pre-order page for a non-existent reservation
      Then the storefront displays a reservation not found error
